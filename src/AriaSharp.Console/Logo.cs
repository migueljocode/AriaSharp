namespace AriaSharp.Console;

public static class Logo
{
    public static readonly string[] Ascii = new[]
    {
        "   ____      _      _     ",
        "  / ___|   / \\    / \\    ",
        " | |  _   / _ \\  / _ \\   ",
        " | |_| | / ___ \\| ___ \\  ",
        "  \\____|/_/   \\_\\_____| ",
        "                         ",
        "   A R I A S H A R P      ",
        "   aria2 client for .NET  ",
    };

    public static void Render(int width, bool stacked = false)
    {
        foreach (var line in Ascii) System.Console.WriteLine(line);
    }
}