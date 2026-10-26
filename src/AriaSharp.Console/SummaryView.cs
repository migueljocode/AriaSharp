using System.Text;

namespace AriaSharp.App;

public static class SummaryView
{
    public static void Render(StringBuilder sb, Models.SessionState state, int width)
    {
        var w = Math.Max(40, width - 4);
        sb.AppendLine("┌" + new string('─', w) + "┐");
        sb.AppendLine("│" + Center("SUMMARY", w) + "│");
        sb.AppendLine("├" + new string('─', w) + "┤");
        sb.AppendLine("│" + $"  active={state.NumActive} waiting={state.NumWaiting} stopped={state.NumStopped} paused={state.NumPaused}".PadRight(w) + "│");
        sb.AppendLine("│" + $"  speed={Models.FormatHelpers.FormatSpeed(state.TotalDownloadSpeed)} completed={Models.FormatHelpers.FormatBytes(state.CompletedLength)} total={Models.FormatHelpers.FormatBytes(state.TotalLength)}".PadRight(w) + "│");
        sb.AppendLine("│" + $"  overall={state.OverallPercent:F1}% eta={state.OverallEta.Format()}".PadRight(w) + "│");
        sb.AppendLine("└" + new string('─', w) + "┘");
    }

    private static string Center(string text, int w)
    {
        if (text.Length >= w) return text[..w];
        var left = (w - text.Length) / 2;
        return new string(' ', left) + text + new string(' ', w - left - text.Length);
    }
}