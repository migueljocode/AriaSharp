namespace AriaSharp.App;

/// <summary>
/// Polls Console.KeyAvailable for cancel keys (q / Q / Esc).
/// Calls CancelAsync once; safe to run as a background Task.
/// </summary>
internal static class KeyListener
{
    public static async Task WatchCancelAsync(CancellationTokenSource cts, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cts);
        if (System.Console.IsInputRedirected)
            return;

        try
        {
            while (!ct.IsCancellationRequested && !cts.IsCancellationRequested)
            {
                if (System.Console.KeyAvailable)
                {
                    var key = System.Console.ReadKey(intercept: true);
                    if (key.Key is ConsoleKey.Q or ConsoleKey.Escape)
                    {
                        await cts.CancelAsync().ConfigureAwait(false);
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
}
