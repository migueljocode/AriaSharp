using System.Net.Http;
namespace AriaSharp.App;

internal enum DashboardKeyAction
{
    None,
    Quit,
    PauseAll,
    ResumeAll,
}

internal static class KeyListener
{
    public static async Task WatchCancelAsync(CancellationTokenSource cts, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cts);
        try
        {
            while (!ct.IsCancellationRequested)
            {
                if (System.Console.KeyAvailable)
                {
                    var key = System.Console.ReadKey(intercept: true);
                    if (key.Key is ConsoleKey.Q or ConsoleKey.Escape)
                    {
                        try { await cts.CancelAsync().ConfigureAwait(false); }
                        catch (ObjectDisposedException) { }
                        return;
                    }
                }
                await Task.Delay(50, ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
        catch (InvalidOperationException) { }
        catch (IOException) { }
    }

    /// <summary>
    /// Dashboard control loop: Q/Esc quit, P pause all, R resume all.
    /// Invokes actions; cancels CTS on quit.
    /// </summary>
    public static async Task WatchDashboardAsync(
        CancellationTokenSource cts,
        Func<DashboardKeyAction, CancellationToken, Task> onAction,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cts);
        ArgumentNullException.ThrowIfNull(onAction);
        try
        {
            while (!ct.IsCancellationRequested)
            {
                if (System.Console.KeyAvailable)
                {
                    var key = System.Console.ReadKey(intercept: true);
                    if (key.Key is ConsoleKey.Q or ConsoleKey.Escape)
                    {
                        try { await onAction(DashboardKeyAction.Quit, ct).ConfigureAwait(false); }
                        catch (Exception ex) when (ex is InvalidOperationException or IOException or ObjectDisposedException or HttpRequestException) { }
                        try { await cts.CancelAsync().ConfigureAwait(false); }
                        catch (ObjectDisposedException) { }
                        return;
                    }

                    var c = char.ToLowerInvariant(key.KeyChar);
                    if (c == 'p' || key.Key == ConsoleKey.P)
                    {
                        try { await onAction(DashboardKeyAction.PauseAll, ct).ConfigureAwait(false); }
                        catch (Exception ex) when (ex is InvalidOperationException or IOException or ObjectDisposedException or HttpRequestException) { }
                    }
                    else if (c == 'r' || key.Key == ConsoleKey.R)
                    {
                        try { await onAction(DashboardKeyAction.ResumeAll, ct).ConfigureAwait(false); }
                        catch (Exception ex) when (ex is InvalidOperationException or IOException or ObjectDisposedException or HttpRequestException) { }
                    }
                }
                await Task.Delay(50, ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
        catch (InvalidOperationException) { }
        catch (IOException) { }
    }

    public static bool WaitRetryOrQuit()
    {
        try
        {
            while (true)
            {
                if (!System.Console.KeyAvailable)
                {
                    Thread.Sleep(30);
                    continue;
                }
                var key = System.Console.ReadKey(intercept: true);
                if (key.Key is ConsoleKey.Q or ConsoleKey.Escape or ConsoleKey.N)
                    return false;
                if (key.Key is ConsoleKey.R or ConsoleKey.Y)
                    return true;
                var c = char.ToLowerInvariant(key.KeyChar);
                if (c is 'q' or 'n') return false;
                if (c is 'r' or 'y') return true;
            }
        }
        catch (InvalidOperationException) { }
        catch (IOException) { }
        return false;
    }

    public static void WaitDismiss()
    {
        try
        {
            while (true)
            {
                if (!System.Console.KeyAvailable)
                {
                    Thread.Sleep(30);
                    continue;
                }
                _ = System.Console.ReadKey(intercept: true);
                return;
            }
        }
        catch (InvalidOperationException) { }
        catch (IOException) { }
    }
}
