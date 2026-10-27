using System.Text.Json;

using Spectre.Console;

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

        // -y / --yes: non-interactive defaults
        if (settings.Yes)
        {
            settings.KeepListFiles = true;
            settings.Resume = true;
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

        // Standalone install: -y / --install-deps / --install-aria2 with no URLs
        var wantsInstall = !settings.NoInstall
            && (settings.InstallDeps || settings.InstallAria2);
        if (wantsInstall && settings.Inputs.Count == 0)
        {
            return await RunInstallOnlyAsync(settings).ConfigureAwait(false);
        }

        // Smart resolve: URLs / files (extract links) / clipboard / stdin
        try
        {
            var resolved = await InputResolver.ResolveAsync(
                    settings.Inputs,
                    new UrlExtractor(),
                    new UrlClassifier(),
                    new ClipboardReader(),
                    forceClipboard: settings.FromClipboard,
                    forceFiles: settings.FromFiles,
                    CancellationToken.None)
                .ConfigureAwait(false);
            settings.Inputs = resolved.ToList();
            if (settings.Verbose && resolved.Count > 0)
            {
                await System.Console.Error.WriteLineAsync(
                        $"Resolved {resolved.Count} download URL(s).")
                    .ConfigureAwait(false);
            }
        }
        catch (FileNotFoundException ex)
        {
            await System.Console.Error.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
            return (int)ExitCode.UsageError;
        }

        if (settings.Inputs.Count == 0)
        {
            await System.Console.Out.WriteLineAsync(
                    "No URLs found (CLI, files, stdin, or clipboard). Use --help, --from-clipboard, or --from-file.")
                .ConfigureAwait(false);
            return (int)ExitCode.UsageError;
        }

        while (true)
        {
            var (code, retry) = await RunDownloadAsync(settings).ConfigureAwait(false);
            if (!retry)
                return code;
            // User pressed R inside the TUI — full re-run with same inputs.
        }
    }

    private static async Task<int> RunInstallOnlyAsync(AppSettings settings)
    {
        await System.Console.Out.WriteLineAsync("Installing prerequisites…").ConfigureAwait(false);
        var installAria2 = settings.InstallDeps || settings.InstallAria2;
        var (ok, message) = await PrerequisiteInstaller.InstallDepsAsync(installAria2, CancellationToken.None)
            .ConfigureAwait(false);
        await System.Console.Out.WriteLineAsync(message).ConfigureAwait(false);
        if (!ok)
        {
            await System.Console.Error.WriteLineAsync("Error: one or more required packages failed to install.").ConfigureAwait(false);
            return (int)ExitCode.Failure;
        }
        await System.Console.Out.WriteLineAsync("Done.").ConfigureAwait(false);
        return (int)ExitCode.Ok;
    }

    private static async Task<(int Code, bool Retry)> RunDownloadAsync(AppSettings settings)
    {
        using var cts = new CancellationTokenSource();
        ConsoleCancelEventHandler onCancel = (_, e) =>
        {
            e.Cancel = true; // keep process alive for graceful shutdown
            try
            {
                if (!cts.IsCancellationRequested)
                    cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // handler can fire after RunDownloadAsync returns
            }
        };
        Console.CancelKeyPress += onCancel;
        try
        {
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
                return ((int)ExitCode.Failure, false);
            }

            string aria2Path;
            try
            {
                var locator = new Aria2Locator(settings.Aria2Path);
                aria2Path = await locator.LocateAsync(cts.Token).ConfigureAwait(false);
            }
            catch (FileNotFoundException ex)
            {
                var mayInstall = !settings.NoInstall
                    && (settings.InstallDeps || settings.InstallAria2 || settings.Yes);
                if (mayInstall)
                {
                    await System.Console.Error.WriteLineAsync("aria2c not found — attempting install…").ConfigureAwait(false);
                    var installed = await PrerequisiteInstaller.InstallAria2Async(cts.Token).ConfigureAwait(false);
                    if (installed)
                    {
                        try
                        {
                            aria2Path = await new Aria2Locator(settings.Aria2Path).LocateAsync(cts.Token).ConfigureAwait(false);
                        }
                        catch (FileNotFoundException retryEx)
                        {
                            await System.Console.Error.WriteLineAsync($"Error: install reported success but aria2c still missing ({retryEx.Message})").ConfigureAwait(false);
                            return ((int)ExitCode.Failure, false);
                        }
                    }
                    else
                    {
                        await System.Console.Error.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
                        return ((int)ExitCode.Failure, false);
                    }
                }
                else
                {
                    await System.Console.Error.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
                    await System.Console.Error.WriteLineAsync("Hint: pass -y / --install-deps / --install-aria2 to install, or --aria2-path.").ConfigureAwait(false);
                    return ((int)ExitCode.Failure, false);
                }
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
                LogFile = workspace.LogFile,
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
                return ((int)ExitCode.Failure, false);
            }

            var useDashboard = !settings.Plain && !settings.Inline && !settings.NoColor && !settings.Ascii
                && !System.Console.IsOutputRedirected
                && AnsiConsole.Profile.Capabilities.Interactive
                && AnsiConsole.Profile.Capabilities.Ansi;

            RunOutcome outcome;
            SessionState finalState;
            TimeSpan elapsed;
            var dashboardRetry = false;

            if (useDashboard)
            {
                Theme.Enabled = true;
                (outcome, finalState, elapsed, dashboardRetry) = await RunWithDashboardAsync(
                    engine, settings, Path.GetFullPath(outputDir), Path.GetFullPath(sessionPath), cts.Token).ConfigureAwait(false);
            }
            else
            {
                var interactive = !System.Console.IsOutputRedirected;
                var useInline = settings.Inline && !settings.Plain;
                // Throttle redirected progress to ~1 Hz; interactive CR updates stay full rate.
                ProgressThrottle? throttle = interactive ? null : new ProgressThrottle(1000);
                ProgressTick? onTick = (state, tickElapsed) =>
                {
                    try
                    {
                        if (throttle is not null && !throttle.ShouldEmit(state, tickElapsed))
                            return;
                        if (useInline)
                            InlineView.WriteProgress(System.Console.Error, state, tickElapsed, color: !settings.NoColor);
                        else
                            PlainView.WriteProgress(System.Console.Error, state, tickElapsed, interactive);
                    }
                    catch (IOException) { }
                };
                var session = new DownloadSession(engine, new ProgressCalculator(), settings, onTick);
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
                    if (useInline) InlineView.EndProgress(System.Console.Error);
                    else PlainView.EndProgress(System.Console.Error, interactive);
                    await System.Console.Error.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
                    return ((int)ExitCode.Failure, false);
                }
                if (useInline) InlineView.EndProgress(System.Console.Error);
                else PlainView.EndProgress(System.Console.Error, interactive);
                finalState = session.State;
                elapsed = session.Elapsed;
            }

            var outDir = Path.GetFullPath(outputDir);
            var sess = Path.GetFullPath(sessionPath);
            var color = !settings.NoColor && !settings.Ascii && !settings.Plain
                && !System.Console.IsOutputRedirected;
            var canInteract = !settings.Yes
                && !settings.Plain
                && !System.Console.IsInputRedirected
                && !System.Console.IsOutputRedirected;
            var canPrompt = outcome.Kind == RunOutcomeKind.Failed && canInteract;

            // One summary on the normal screen (after alt-screen is gone) — no double print, no Q-dismiss glitch.
            SummaryView.Write(
                finalState, outcome, elapsed, outDir, sess, color,
                showRetryPrompt: canPrompt);

            var retry = false;
            if (canPrompt)
                retry = KeyListener.WaitRetryOrQuit();

            if (outcome.Kind == RunOutcomeKind.Failed && !string.IsNullOrEmpty(outcome.Message))
                await System.Console.Error.WriteLineAsync($"Error: {outcome.Message}").ConfigureAwait(false);
            else if (outcome.Kind == RunOutcomeKind.Empty && !string.IsNullOrEmpty(outcome.Message))
                await System.Console.Error.WriteLineAsync($"Error: {outcome.Message}").ConfigureAwait(false);

            // Drop empty resume.session (agreed: no empty session left behind)
            try
            {
                if (!string.IsNullOrEmpty(sess) && File.Exists(sess) && !SessionFile.HasEntries(sess))
                    await SessionFile.DeleteAsync(sess).ConfigureAwait(false);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }

            var code = outcome.Kind switch
            {
                RunOutcomeKind.Completed => (int)ExitCode.Ok,
                RunOutcomeKind.Interrupted => (int)ExitCode.Interrupted,
                // Empty (no items) is a failure for the user, not success
                RunOutcomeKind.Empty => (int)ExitCode.Failure,
                _ => (int)ExitCode.Failure,
            };
            return (code, retry);
        }
        finally
        {
            await engine.DisposeAsync().ConfigureAwait(false);
            await workspace.DisposeAsync().ConfigureAwait(false);
        }
        }
        finally
        {
            Console.CancelKeyPress -= onCancel;
        }
    }

    private static async Task<(RunOutcome Outcome, SessionState State, TimeSpan Elapsed, bool Retry)> RunWithDashboardAsync(
        Aria2Engine engine,
        AppSettings settings,
        string outputDir,
        string sessionPath,
        CancellationToken ct)
    {
        RunOutcome outcome = RunOutcome.Interrupted();
        DownloadSession? session = null;

        var empty = new SessionState
        {
            ActiveItems = [],
            WaitingItems = [],
            StoppedItems = [],
            PausedItems = [],
            CompleteItems = [],
            FailedItems = [],
            SkippedItems = [],
            InterruptedItems = [],
            AllItems = [],
        };

        if (!ScreenGuard.EnsureMinimum(60, 12))
            ScreenGuard.ShowWarning(60, 12);

        // Defer alt-screen until we actually have downloads — avoids empty flash on bad clipboard.
        ScreenGuard? guard = null;
        var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
        try
        {
            var keyTask = KeyListener.WatchDashboardAsync(
                linked,
                async (action, token) =>
                {
                    try
                    {
                        switch (action)
                        {
                            case DashboardKeyAction.PauseAll:
                                await engine.PauseAllAsync(token).ConfigureAwait(false);
                                break;
                            case DashboardKeyAction.ResumeAll:
                                await engine.UnpauseAllAsync(token).ConfigureAwait(false);
                                break;
                            case DashboardKeyAction.Quit:
                                break;
                        }
                    }
                    catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or IOException or ObjectDisposedException)
                    {
                        // best-effort controls
                    }
                },
                linked.Token);
            try
            {
                await AnsiConsole.Live(DashboardView.Build(empty, TimeSpan.Zero, settings.Split))
                    .AutoClear(true)
                    .Overflow(VerticalOverflow.Ellipsis)
                    .Cropping(VerticalOverflowCropping.Bottom)
                    .StartAsync(async ctx =>
                    {
                        var loop = new DashboardLoop(ctx, settings.Split);
                        using var renderCts = CancellationTokenSource.CreateLinkedTokenSource(linked.Token);
                        var render = loop.RunAsync(renderCts.Token);
                        session = new DownloadSession(engine, new ProgressCalculator(), settings, loop.Publish);
                        try
                        {
                            outcome = await session.RunAsync(linked.Token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            try { await session.RequestStopAsync().ConfigureAwait(false); }
                            catch (InvalidOperationException) { }
                            catch (IOException) { }
                            outcome = RunOutcome.Interrupted("Cancelled.");
                        }
                        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or IOException)
                        {
                            outcome = RunOutcome.Failed(ex.Message);
                        }
                        finally
                        {
                            await renderCts.CancelAsync().ConfigureAwait(false);
                            await render.ConfigureAwait(false);
                            guard = loop.Guard;
                        }
                        // Do not hold summary inside Live/alt-screen — leaves a clean buffer.
                    }).ConfigureAwait(false);
            }
            finally
            {
                if (!linked.IsCancellationRequested)
                {
                    try { await linked.CancelAsync().ConfigureAwait(false); }
                    catch (ObjectDisposedException) { }
                }
                try { await keyTask.ConfigureAwait(false); }
                catch (OperationCanceledException) { }
            }
        }
        finally
        {
            linked.Dispose();
            guard?.Dispose();
        }

        // Retry is decided on the normal screen after return (single correct summary).
        return (outcome, session?.State ?? empty, session?.Elapsed ?? TimeSpan.Zero, false);
    }
}
