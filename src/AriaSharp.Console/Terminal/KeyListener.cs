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
        ThrowIfNull(cts);
        try
        {
            while (!ct.IsCancellationRequested)
            {
                if (Console.KeyAvailable && Console.ReadKey(intercept: true).Key is ConsoleKey.Q or ConsoleKey.Escape)
                {
                    try { await cts.CancelAsync().ConfigureAwait(false); }
                    catch (ObjectDisposedException) { }
                    return;
                }

                await Task.Delay(50, ct).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or InvalidOperationException or IOException)
        {
        }
    }

    /// <summary>
    /// Dashboard control loop: Q/Esc quit, P pause all, R resume all.
    /// </summary>
    public static async Task WatchDashboardAsync(
        CancellationTokenSource cts,
        Func<DashboardKeyAction, CancellationToken, Task> onAction,
        CancellationToken ct)
    {
        ThrowIfNull(cts);
        ThrowIfNull(onAction);
        try
        {
            while (!ct.IsCancellationRequested)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true);
                    var action = key.Key switch
                    {
                        ConsoleKey.Q or ConsoleKey.Escape => DashboardKeyAction.Quit,
                        ConsoleKey.P => DashboardKeyAction.PauseAll,
                        ConsoleKey.R => DashboardKeyAction.ResumeAll,
                        _ => char.ToLowerInvariant(key.KeyChar) switch
                        {
                            'p' => DashboardKeyAction.PauseAll,
                            'r' => DashboardKeyAction.ResumeAll,
                            'q' => DashboardKeyAction.Quit,
                            _ => DashboardKeyAction.None,
                        },
                    };

                    if (action is DashboardKeyAction.None)
                    {
                        await Task.Delay(50, ct).ConfigureAwait(false);
                        continue;
                    }

                    try
                    {
                        await onAction(action, ct).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or IOException or ObjectDisposedException or HttpRequestException)
                    {
                    }

                    if (action is DashboardKeyAction.Quit)
                    {
                        try { await cts.CancelAsync().ConfigureAwait(false); }
                        catch (ObjectDisposedException) { }
                        return;
                    }
                }

                await Task.Delay(50, ct).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or InvalidOperationException or IOException)
        {
        }
    }

    public static bool WaitRetryOrQuit()
    {
        try
        {
            while (true)
            {
                if (!Console.KeyAvailable)
                {
                    Thread.Sleep(30);
                    continue;
                }

                var key = Console.ReadKey(intercept: true);
                switch (key.Key)
                {
                    case ConsoleKey.Q or ConsoleKey.Escape or ConsoleKey.N:
                        return false;
                    case ConsoleKey.R or ConsoleKey.Y:
                        return true;
                }
                switch (char.ToLowerInvariant(key.KeyChar))
                {
                    case 'q' or 'n':
                        return false;
                    case 'r' or 'y':
                        return true;
                }
                // ignore other keys
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException)
        {
            return false;
        }
    }

    public static void WaitDismiss()
    {
        try
        {
            while (true)
            {
                if (!Console.KeyAvailable)
                {
                    Thread.Sleep(30);
                    continue;
                }

                _ = Console.ReadKey(intercept: true);
                return;
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException)
        {
        }
    }
}
