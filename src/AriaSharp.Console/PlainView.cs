using System.Text;

namespace AriaSharp.App;

public static class PlainView
{
    public static void Render(StringBuilder sb, Models.SessionState state, int width)
    {
        var w = Math.Max(40, width - 4);
        sb.AppendLine("┌" + new string('─', w) + "┐");
        sb.AppendLine("│" + Center("PLAIN VIEW", w) + "│");
        sb.AppendLine("├" + new string('─', w) + "┤");
        var total = state.AllItems.Count;
        var active = state.ActiveItems.Count;
        var waiting = state.WaitingItems.Count;
        var stopped = state.StoppedItems.Count;
        var paused = state.PausedItems.Count;
        var complete = state.CompleteItems.Count;
        var failed = state.FailedItems.Count;
        var skipped = state.SkippedItems.Count;
        var interrupted = state.InterruptedItems.Count;
        sb.AppendLine("│" + $"  total={total} active={active} waiting={waiting} stopped={stopped} paused={paused} complete={complete} failed={failed} skipped={skipped} interrupted={interrupted}".PadRight(w) + "│");
        sb.AppendLine("│" + $"  speed={Models.FormatHelpers.FormatSpeed(state.TotalDownloadSpeed)} overall={state.OverallPercent:F1}% eta={state.OverallEta.Format()}".PadRight(w) + "│");
        sb.AppendLine("└" + new string('─', w) + "┘");
    }

    private static string Center(string text, int w)
    {
        if (text.Length >= w) return text[..w];
        var left = (w - text.Length) / 2;
        return new string(' ', left) + text + new string(' ', w - left - text.Length);
    }
}