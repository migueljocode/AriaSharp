namespace AriaSharp.App;

/// <summary>
/// Purple-gradient terminal theme. Spectre colour names / hex for truecolor.
/// </summary>
internal static class Theme
{
    public static bool Enabled { get; set; } = true;

    // Brand purple spectrum
    public const string Purple = "#9B59B6";
    public const string PurpleBright = "#BB86FC";
    public const string PurpleDeep = "#6C3483";
    public const string Magenta = "#E040FB";
    public const string Accent = "#CE93D8";
    public const string Success = "green";
    public const string Warn = "yellow";
    public const string Error = "red";
    public const string Muted = "grey";
    public const string Title = "white";
    public const string Dim = "grey23";

    public static string Markup(string colour, string text) =>
        Enabled ? $"[{colour}]{text.Replace("[", "[[").Replace("]", "]]")}[/]" : text;
}
