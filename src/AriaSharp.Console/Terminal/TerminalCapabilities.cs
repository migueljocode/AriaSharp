
namespace AriaSharp.App;

internal static class Terminal
{
    public static int Width { get; set; } = 100;
    public static int Height { get; set; } = 30;
    public static bool IsAlternateScreenSupported { get; set; } = true;
    public static bool UseColor { get; set; } = true;

    public static void EnterAlternate() { if (IsAlternateScreenSupported) System.Console.Write("\x1b[?1049h"); }
    public static void ExitAlternate() { if (IsAlternateScreenSupported) System.Console.Write("\x1b[?1049l"); }
    public static void Clear() { System.Console.Clear(); }
    public static void Write(string text) { System.Console.Write(text); }
    public static void WriteLine(string text) { System.Console.WriteLine(text); }
    public static void WriteColored(string text, string color)
    {
        if (!UseColor) { System.Console.Write(text); return; }
        var code = color switch
        {
            "red" => "31", "green" => "32", "yellow" => "33", "blue" => "34", "magenta" => "35", "cyan" => "36", "white" => "37", "grey" => "90", "darkgrey" => "90",
            _ => "0"
        };
        System.Console.Write($"\x1b[{code}m{text}\x1b[0m");
    }
    public static string Truncate(string? text, int max) => Truncate(text, max);
}