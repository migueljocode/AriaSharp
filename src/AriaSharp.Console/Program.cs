using System.Text.Json;

using AriaSharp.Models;
using AriaSharp.Services;

namespace AriaSharp.App;

/// <summary>
/// Composition root. Wires defaults → config → env → CLI, then hands off to a real
/// <see cref="Aria2Engine"/> + <see cref="DownloadSession"/>.
/// </summary>
internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        SettingsOverlay cli;
        try
        {
            cli = ArgParser.Parse(args);
        }
        catch (CliException ex)
        {
            await System.Console.Error.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
            return (int)ExitCode.UsageError;
        }

        var store = new SettingsStore();
        var configFile = cli.ConfigFile ?? AppPaths.SettingsFile;
        SettingsOverlay configOverlay;
        try
        {
            configOverlay = await store.LoadOverlayAsync(configFile, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or DirectoryNotFoundException or IOException)
        {
            await System.Console.Error.WriteLineAsync($"Warning: could not read config file {configFile} ({ex.Message})").ConfigureAwait(false);
            configOverlay = new SettingsOverlay();
        }

        var envOverlay = EnvOverlay.FromEnvironment();
        var merger = new SettingsMerger();
        var settings = merger.Merge(new AppSettings(), configOverlay, envOverlay, cli);

        if (cli.NoInstall == true)
        {
            settings.InstallDeps = false;
            settings.InstallAria2 = false;
        }

        if (settings.Force)
            settings.AllowOverwrite = true;

        if (cli.Help == true)
        {
            Theme.Enabled = !(settings.NoColor || settings.Ascii || settings.Plain);
            HelpView.Render(settings);
            return (int)ExitCode.Ok;
        }

        if (cli.Version == true)
        {
            await System.Console.Out.WriteLineAsync("AriaSharp 1.0.0").ConfigureAwait(false);
            return (int)ExitCode.Ok;
        }

        if (cli.PrintConfig == true)
        {
            var json = JsonSerializer.Serialize(settings, AppSettingsJsonContext.Default.AppSettings);
            await System.Console.Out.WriteLineAsync(json).ConfigureAwait(false);
            return (int)ExitCode.Ok;
        }

        if (settings.Inputs.Count == 0)
        {
            await System.Console.Out.WriteLineAsync("No inputs provided. Use --help for usage.").ConfigureAwait(false);
            return (int)ExitCode.UsageError;
        }

        return await RunDownloadAsync(settings).ConfigureAwait(false);
    }

    private static async Task<int> RunDownloadAsync(AppSettings settings)
    {
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        var outputDir = string.IsNullOrWhiteSpace(settings.OutputDir)
            ? AppDefaults.OutputDirDefault
            : settings.OutputDir;
        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        var sessionPath = string.IsNullOrWhiteSpace(settings.SessionFile)
            ? Path.Join(outputDir, AppDefaults.SessionFileName)
            : settings.SessionFile;

        var workspace = await RunWorkspace.CreateAsync(cts.Token).ConfigureAwait(false);
        var engine = new Aria2Engine();
        try
        {
            try
            {
                await InputMerger.WriteAsync(settings.Inputs.ToArray(), workspace.InputFile, cts.Token)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                await System.Console.Error.WriteLineAsync($"Error: could not write input file ({ex.Message})").ConfigureAwait(false);
                return (int)ExitCode.Failure;
            }

            string aria2Path;
            try
            {
                var locator = new Aria2Locator(settings.Aria2Path);
                aria2Path = await locator.LocateAsync(cts.Token).ConfigureAwait(false);
            }
            catch (FileNotFoundException ex)
            {
                await System.Console.Error.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
                return (int)ExitCode.Failure;
            }

            var port = Aria2Engine.FindFreePort();
            var secret = Aria2Engine.CreateSecret();
            var provisional = new EngineSettings
            {
                Aria2Path = aria2Path,
                OutputDir = Path.GetFullPath(outputDir),
                SessionFile = Path.GetFullPath(sessionPath),
                InputFile = workspace.InputFile,
                ConfFile = workspace.ConfFile,
                Secret = secret,
                Port = port,
                Args = Array.Empty<string>(),
            };
            var args = new Aria2ArgsBuilder().Build(provisional, settings);
            var engineSettings = new EngineSettings
            {
                Aria2Path = provisional.Aria2Path,
                OutputDir = provisional.OutputDir,
                SessionFile = provisional.SessionFile,
                InputFile = provisional.InputFile,
                ConfFile = provisional.ConfFile,
                Secret = provisional.Secret,
                Port = provisional.Port,
                Args = args,
            };

            try
            {
                await engine.StartAsync(engineSettings, cts.Token).ConfigureAwait(false);
                if (settings.Verbose)
                {
                    var version = await engine.GetVersionAsync(cts.Token).ConfigureAwait(false);
                    await System.Console.Error.WriteLineAsync($"aria2 {version} on 127.0.0.1:{port}").ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException or TimeoutException or HttpRequestException or IOException)
            {
                await System.Console.Error.WriteLineAsync($"Error: failed to start aria2 ({ex.Message})").ConfigureAwait(false);
                return (int)ExitCode.Failure;
            }

            var session = new DownloadSession(engine, new ProgressCalculator(), settings);
            RunOutcome outcome;
            try
            {
                outcome = await session.RunAsync(cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                outcome = RunOutcome.Interrupted();
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or IOException)
            {
                await System.Console.Error.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
                return (int)ExitCode.Failure;
            }

            var summary = SummaryView.Render(
                session.State,
                outcome,
                session.Elapsed,
                Path.GetFullPath(outputDir),
                Path.GetFullPath(sessionPath));
            await System.Console.Out.WriteAsync(summary).ConfigureAwait(false);

            if (outcome.Kind == RunOutcomeKind.Failed && !string.IsNullOrEmpty(outcome.Message))
                await System.Console.Error.WriteLineAsync($"Error: {outcome.Message}").ConfigureAwait(false);

            return outcome.Kind switch
            {
                RunOutcomeKind.Completed => (int)ExitCode.Ok,
                RunOutcomeKind.Interrupted => (int)ExitCode.Interrupted,
                RunOutcomeKind.Empty => (int)ExitCode.Ok,
                _ => (int)ExitCode.Failure,
            };
        }
        finally
        {
            await engine.DisposeAsync().ConfigureAwait(false);
            await workspace.DisposeAsync().ConfigureAwait(false);
        }
    }
}
