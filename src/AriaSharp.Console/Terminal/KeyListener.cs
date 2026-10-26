namespace AriaSharp.App;

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
    /// Single key: R/Y = retry, Q/Esc/N = quit. No Enter required.
    /// </summary>
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

    /// <summary>Wait for any key (or Q/Esc) to dismiss a held screen. No Enter.</summary>
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
