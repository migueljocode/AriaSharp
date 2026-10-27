namespace AriaSharp.App;

/// <summary>
/// Download session orchestration: engine lifecycle, views, summary + retry.
/// </summary>
internal static class DownloadCommand
{
    public static async Task<(int Code, bool Retry)> RunAsync(AppSettings settings)
    {
        using var cts = new CancellationTokenSource();
        var cancelCount = 0;
        ConsoleCancelEventHandler onCancel = (_, e) =>
        {
            e.Cancel = true;
            cancelCount++;
            if (cancelCount >= 2)
            {
                e.Cancel = false; // second Ctrl-C: hard exit, restore terminal
                return;
            }
            try
            {
                if (!cts.IsCancellationRequested)
                    cts.Cancel();
            }
            catch (ObjectDisposedException) { }
        };
        Console.CancelKeyPress += onCancel;
        try
        {
            var (outputDir, sessionPath) = DownloadPaths.Resolve(settings);
            var workspace = await RunWorkspace.CreateAsync(cts.Token);
            var engine = new Aria2Engine();
            try
            {
                try
                {
                    await InputMerger.WriteAsync(settings.Inputs.ToArray(), workspace.InputFile, cts.Token);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    await Console.Error.WriteLineAsync($"Error: could not write input file ({ex.Message})");
                    return ((int)ExitCode.Failure, false);
                }

                var (aria2Path, fail) = await Aria2Bootstrap.LocateOrInstallAsync(settings, cts.Token);
                if (fail is int code || aria2Path is null)
                    return (fail ?? (int)ExitCode.Failure, false);

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
                    Args = [],
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
                    await engine.StartAsync(engineSettings, cts.Token);
                    if (settings.Verbose)
                    {
                        var version = await engine.GetVersionAsync(cts.Token);
                        await Console.Error.WriteLineAsync($"aria2 {version} on 127.0.0.1:{port}");
                    }
                }
                catch (Exception ex) when (ex is IOException or InvalidOperationException or HttpRequestException)
                {
                    await Console.Error.WriteLineAsync($"Error: failed to start aria2 ({ex.Message})");
                    return ((int)ExitCode.Failure, false);
                }

                RunOutcome outcome;
                SessionState finalState;
                TimeSpan elapsed;
                var useDashboard = !settings.Plain && !settings.Inline && !Console.IsOutputRedirected;

                if (useDashboard)
                {
                    (outcome, finalState, elapsed, _) = await DashboardRunner.RunWithDashboardAsync(
                        engine, settings, outputDir, sessionPath, cts.Token);
                }
                else
                {
                    (outcome, finalState, elapsed) = await RunPlainOrInlineAsync(
                        engine, settings, cts.Token);
                }

                return await DownloadSummary.FinishAsync(
                    finalState, outcome, elapsed,
                    Path.GetFullPath(outputDir), Path.GetFullPath(sessionPath), settings);
            }
            finally
            {
                await engine.DisposeAsync();
                await workspace.DisposeAsync();
            }
        }
        finally
        {
            Console.CancelKeyPress -= onCancel;
        }
    }

    private static async Task<(RunOutcome Outcome, SessionState State, TimeSpan Elapsed)> RunPlainOrInlineAsync(
        Aria2Engine engine,
        AppSettings settings,
        CancellationToken ct)
    {
        var useInline = settings.Inline && !Console.IsOutputRedirected;
        var interactive = !Console.IsOutputRedirected;
        var session = new DownloadSession(engine, new ProgressCalculator(), settings, null);
        try
        {
            var progressTask = Task.Run(async () =>
            {
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(settings.RefreshIntervalMs, ct);
                    }
                    catch (OperationCanceledException) { break; }

                    var state = session.State;
                    var elapsed = session.Elapsed;
                    if (useInline)
                        InlineView.WriteProgress(Console.Error, state, elapsed, !settings.NoColor);
                    else
                        PlainView.WriteProgress(Console.Error, state, elapsed, interactive);
                }
            }, ct);

            RunOutcome outcome;
            try
            {
                outcome = await session.RunAsync(ct);
            }
            catch (OperationCanceledException)
            {
                try { await session.RequestStopAsync(); }
                catch (Exception ex) when (ex is InvalidOperationException or IOException) { }
                outcome = RunOutcome.Interrupted("Cancelled.");
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or IOException)
            {
                if (useInline) InlineView.EndProgress(Console.Error);
                else PlainView.EndProgress(Console.Error, interactive);
                await Console.Error.WriteLineAsync($"Error: {ex.Message}");
                return (RunOutcome.Failed(ex.Message), session.State, session.Elapsed);
            }

            if (useInline) InlineView.EndProgress(Console.Error);
            else PlainView.EndProgress(Console.Error, interactive);

            try { await progressTask; }
            catch (OperationCanceledException) { }

            return (outcome, session.State, session.Elapsed);
        }
        finally
        {
            // session is not IDisposable
        }
    }
}
