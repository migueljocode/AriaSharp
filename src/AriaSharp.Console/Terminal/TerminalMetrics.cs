namespace AriaSharp.App;

/// <summary>Shared terminal size probes (DRY for views and pager).</summary>
internal static class TerminalMetrics
{
    public static int WindowWidth(int fallback = 80)
    {
        try
        {
            var w = Console.WindowWidth;
            return w > 0 ? w : fallback;
        }
        catch (IOException) { return fallback; }
        catch (InvalidOperationException) { return fallback; }
    }
}
