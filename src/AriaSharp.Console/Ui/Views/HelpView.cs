using Spectre.Console;

namespace AriaSharp.App;

/// <summary>
/// Help entry point: builds the OptionTable-driven document and opens the pager
/// (or prints it when <c>--no-pager</c> / non-TTY).
/// </summary>
internal static class HelpView
{
    public static void Render(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var document = HelpDocument.Render(settings);
        HelpPager.Show(document, noPager: settings.NoPager || settings.Plain);
    }
}
