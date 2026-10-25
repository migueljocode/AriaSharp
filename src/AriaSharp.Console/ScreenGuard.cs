namespace AriaSharp.Console;

public static class ScreenGuard
{
    public static bool EnsureMinimum(int width, int height)
    {
        try
        {
            if (System.Console.WindowWidth < width || System.Console.WindowHeight < height) return false;
            return true;
        }
        catch (Exception) { return false; }
    }

    public static void ShowWarning(int width, int height)
    {
        System.Console.WriteLine($"Terminal too small (need {width}x{height}).");
    }
}