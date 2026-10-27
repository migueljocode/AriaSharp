namespace AriaSharp.App;

/// <summary>
/// Static entry for tests / <c>global using static</c>. Implementation: <c>args.RunAriaSharpAsync()</c>.
/// </summary>
internal static class AppHost
{
    public static Task<int> RunAsync(string[] args) => args.RunAriaSharpAsync();
}
