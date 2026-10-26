using System.Text;

namespace AriaSharp.App;

internal static class DashboardView
{
    public static void Render(StringBuilder sb, Models.SessionState state, int width)
    {
        ArgumentNullException.ThrowIfNull(sb);
        ArgumentNullException.ThrowIfNull(state);
        var w = Math.Max(40, width - 4);
        sb.AppendLine("┌" + new string('─', w) + "┐");
        sb.AppendLine("│" + Center("ACTIVE DOWNLOADS", w) + "│");
        sb.AppendLine("├" + new string('─', w) + "┤");
        if (state.ActiveItems.Count == 0)
        {
            sb.AppendLine("│" + Center("(none)", w) + "│");
        }
        else
        {
            foreach (var item in state.ActiveItems)
            {
                var pct = item.TotalLength > 0 ? (double)item.CompletedLength / item.TotalLength * 100 : 0;
                var line = $" {Models.FormatHelpers.Truncate(item.Name, 28)} {pct,6:F1}% {Models.FormatHelpers.FormatSpeed(item.DownloadSpeed)}";
                sb.AppendLine("│" + line.PadRight(w) + "│");
            }
        }
        sb.AppendLine("└" + new string('─', w) + "┘");
    }

    private static string Center(string text, int w)
    {
        if (text.Length >= w) return text[..w];
        var left = (w - text.Length) / 2;
        return new string(' ', left) + text + new string(' ', w - left - text.Length);
    }
}