using System.Text.Json;
using System.Text.Json.Serialization;

using AriaSharp.Models;
using AriaSharp.Services;

namespace AriaSharp.App;

/// <summary>
/// Composition root. Wires the layers (defaults → config file → environment → CLI), dispatches
/// the run-mode flags, and (once the engine exists) hands off to <see cref="DownloadSession"/>.
/// Until M2 the engine is a stub, so a real invocation exits 1 with an honest message.
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

        if (cli.Help == true)
        {
            await System.Console.Out.WriteLineAsync(HelpDocument.Render(settings)).ConfigureAwait(false);
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

        // M2: hand off to DownloadSession against a real aria2 engine. Until then, be honest.
        await System.Console.Error.WriteLineAsync("engine not implemented yet").ConfigureAwait(false);
        return (int)ExitCode.Failure;
    }
}
