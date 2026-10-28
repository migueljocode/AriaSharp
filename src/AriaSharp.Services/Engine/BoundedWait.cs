namespace AriaSharp.Services;

/// <summary>Hard time caps for shutdown work: abandon the wait, never block past the cap.</summary>
internal static class BoundedWait
{
    /// <summary>
    /// Waits for <paramref name="task"/> up to <paramref name="cap"/>. Returns true when it finished
    /// in time (the caller awaits it to observe its result). On timeout the task is abandoned and any
    /// later fault is observed so it cannot surface as an unobserved exception.
    /// </summary>
    public static async Task<bool> WaitAsync(Task task, TimeSpan cap, CancellationToken ct = default)
    {
        using var delayCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var delay = Task.Delay(cap, delayCts.Token);
        var winner = await Task.WhenAny(task, delay);
        if (ReferenceEquals(winner, task))
        {
            await delayCts.CancelAsync();
            return true;
        }

        _ = task.ContinueWith(
            static t => { _ = t.Exception; },
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
        return false;
    }

    /// <summary>
    /// Runs <paramref name="work"/> under a linked token that cancels after <paramref name="cap"/>
    /// and also stops waiting at the cap if the work ignores its token. Best-effort: any
    /// cancellation (the cap, or an already-cancelled <paramref name="ct"/> after Ctrl-C) is swallowed.
    /// </summary>
    public static async Task RunAsync(Func<CancellationToken, Task> work, TimeSpan cap, CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(cap);
        var running = work(cts.Token);
        try
        {
            if (!await WaitAsync(running, cap, ct))
            {
                await cts.CancelAsync();
                return;
            }

            await running;
        }
        catch (OperationCanceledException)
        {
        }
    }
}
