using System.Text;

namespace AriaSharp.Console;

public static class Widgets
{
    public static void RenderSummaryCard(StringBuilder sb, Models.SessionState state, int width)
    {
        var w = Math.Max(20, width - 4);
        sb.AppendLine("┌" + new string('─', w) + "┐");
        sb.AppendLine("│" + Center("SUMMARY", w) + "│");
        sb.AppendLine("├" + new string('─', w) + "┤");
        sb.AppendLine("│" + Row("Active", state.NumActive.ToString(), w) + "│");
        sb.AppendLine("│" + Row("Waiting", state.NumWaiting.ToString(), w) + "│");
        sb.AppendLine("│" + Row("Stopped", state.NumStopped.ToString(), w) + "│");
        sb.AppendLine("│" + Row("Paused", state.NumPaused.ToString(), w) + "│");
        sb.AppendLine("│" + Row("Speed", Models.FormatHelpers.FormatSpeed(state.TotalDownloadSpeed), w) + "│");
        sb.AppendLine("│" + Row("Overall", state.OverallPercent.ToString("F1") + "%", w) + "│");
        sb.AppendLine("│" + Row("ETA", state.OverallEta.Format(), w) + "│");
        sb.AppendLine("└" + new string('─', w) + "┘");
    }

    private static string Row(string label, string value, int w)
    {
        var content = $" {label}: {value} ";
        return content.PadRight(w) + "│";
    }

    private static string Center(string text, int w)
    {
        if (text.Length >= w) return text[..w];
        var left = (w - text.Length) / 2;
        return new string(' ', left) + text + new string(' ', w - left - text.Length);
    }
}