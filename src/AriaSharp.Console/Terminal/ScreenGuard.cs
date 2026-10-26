namespace AriaSharp.App;

/// <summary>
/// Enters the alternate screen buffer + hides the cursor for full-screen UI.
/// Dispose always restores the previous screen and cursor, even on crash paths.
/// </summary>
internal sealed class ScreenGuard : IDisposable
{
    private readonly bool _entered;
    private bool _disposed;

    private ScreenGuard(bool entered) => _entered = entered;

    public static ScreenGuard? TryEnter()
    {
        if (!System.Console.IsOutputRedirected
            && Terminal.IsAlternateScreenSupported
            && AnsiConsoleAvailable())
        {
            try
            {
                System.Console.Write("\x1b[?1049h\x1b[?25l");
                System.Console.Out.Flush();
                return new ScreenGuard(entered: true);
            }
            catch (IOException)
            {
                return null;
            }
        }
        return null;
    }

    public static bool EnsureMinimum(int width, int height)
    {
        try
        {
            return System.Console.WindowWidth >= width && System.Console.WindowHeight >= height;
        }
        catch (IOException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public static void ShowWarning(int width, int height) =>
        System.Console.Error.WriteLine($"Terminal too small (need {width}x{height}). Falling back to plain progress.");

    private static bool AnsiConsoleAvailable()
    {
        try
        {
            return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TERM"))
                || OperatingSystem.IsWindows();
        }
        catch (System.Security.SecurityException)
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (!_entered) return;
        try
        {
            System.Console.Write("\x1b[?25h\x1b[?1049l");
            System.Console.Out.Flush();
        }
        catch (IOException) { }
    }
}
