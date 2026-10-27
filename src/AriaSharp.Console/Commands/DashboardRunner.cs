namespace AriaSharp.App;

/// <summary>
/// Live Spectre dashboard session (alt-screen, keys, render loop).
/// </summary>
internal static class DashboardRunner
{
    public static async Task<(RunOutcome Outcome, SessionState State, TimeSpan Elapsed, bool Retry)> RunWithDashboardAsync(
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
                                await engine.PauseAllAsync(token);
                                break;
                            case DashboardKeyAction.ResumeAll:
                                await engine.UnpauseAllAsync(token);
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
                            outcome = await session.RunAsync(linked.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            try { await session.RequestStopAsync(); }
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
                            await renderCts.CancelAsync();
                            await render;
                            guard = loop.Guard;
                        }
                        // Do not hold summary inside Live/alt-screen — leaves a clean buffer.
                    });
            }
            finally
            {
                if (!linked.IsCancellationRequested)
                {
                    try { await linked.CancelAsync(); }
                    catch (ObjectDisposedException) { }
                }
                try { await keyTask; }
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
