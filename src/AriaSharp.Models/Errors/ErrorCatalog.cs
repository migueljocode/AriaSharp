namespace AriaSharp.Models;

/// <summary>
/// Static entry points for <c>global using static</c>. Implementation lives in <see cref="ErrorExtensions"/>.
/// </summary>
public static class ErrorCatalog
{
    public static string TextFor(int code) => code.Aria2Text();
    public static string HintFor(int code) => code.Aria2Hint();
    public static Aria2ErrorKind Classify(int code) => code.Aria2Kind();
    public static bool IsSkippedCode(int code) => code.IsAria2Skipped();
}
