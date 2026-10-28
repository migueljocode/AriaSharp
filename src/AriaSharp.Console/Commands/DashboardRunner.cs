namespace AriaSharp.App;

/// <summary>
/// Live Spectre dashboard session (keys + render loop). Kept small via helpers.
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
        var empty = EmptyState();
        if (!ScreenGuard.EnsureMinimum(60, 12))
            ScreenGuard.ShowWarning(60, 12);

        ScreenGuard? guard = null;
        DownloadSession? session = null;
        RunOutcome outcome = RunOutcome.Interrupted();

        var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var keyTask = KeyListener.WatchDashboardAsync(
            linked,
            (action, token) => HandleKeyAsync(engine, action, token),
            linked.Token);

        try
        {
            // Poll once before opening the alt screen. If aria2 reports zero items,
            // print the error on the normal screen and never open the alt screen.
            var firstSnapshot = await engine.GetSnapshotAsync(linked.Token);
            if (firstSnapshot.NumActive == 0 && firstSnapshot.NumWaiting == 0 && firstSnapshot.TotalLength == 0)
            {
                await Console.Error.WriteLineAsync("Error: no downloads started — aria2 reported zero items.");
                await StopKeysAsync(linked, keyTask);
                return (RunOutcome.Empty("no downloads started — aria2 reported zero items."), empty, TimeSpan.Zero, false);
            }

            try
            {
                await AnsiConsole.Live(DashboardView.Build(empty, TimeSpan.Zero, settings.MaxConnectionPerServer))
                    .AutoClear(true)
                    .Overflow(VerticalOverflow.Ellipsis)
                    .Cropping(VerticalOverflowCropping.Bottom)
                    .StartAsync(async ctx =>
                    {
                        (outcome, session, guard) = await RunLiveAsync(
                            ctx, engine, settings, linked.Token);
                    });
            }
            finally
            {
                await StopKeysAsync(linked, keyTask);
            }
        }
        finally
        {
            linked.Dispose();
            guard?.Dispose();
        }

        return (outcome, session?.State ?? empty, session?.Elapsed ?? TimeSpan.Zero, false);
    }

    private static SessionState EmptyState() => new()
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

    private static async Task HandleKeyAsync(Aria2Engine engine, DashboardKeyAction action, CancellationToken token)
    {
        try
        {
            switch (action)
            {
                case DashboardKeyAction.PauseAll:
                    await engine.ForcePauseAllAsync(token);
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
    }

    private static async Task<(RunOutcome Outcome, DownloadSession? Session, ScreenGuard? Guard)> RunLiveAsync(
        LiveDisplayContext ctx,
        Aria2Engine engine,
        AppSettings settings,
        CancellationToken ct)
    {
        var loop = new DashboardLoop(ctx, settings.MaxConnectionPerServer);
        using var renderCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var render = loop.RunAsync(renderCts.Token);
        var session = new DownloadSession(engine, new ProgressCalculator(), settings, loop.Publish);
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
            outcome = RunOutcome.Failed(ex.Message);
        }
        finally
        {
            await renderCts.CancelAsync();
            await render;
        }

        return (outcome, session, loop.Guard);
    }

    private static async Task StopKeysAsync(CancellationTokenSource linked, Task keyTask)
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
