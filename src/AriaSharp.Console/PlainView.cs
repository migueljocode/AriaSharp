using System.Text;

namespace AriaSharp.Console;

public static class PlainView
{
    public static void Render(StringBuilder sb, Models.SessionState state, int width)
    {
        var w = Math.Max(40, width - 4);
        sb.AppendLine("┌" + new string('─', w) + "┐");
        sb.AppendLine("│" + Center("PLAIN VIEW", w) + "│");
        sb.AppendLine("├" + new string('─', w) + "┤");
        var total = state.AllItems.Length;
        var active = state.ActiveItems.Length;
        var waiting = state.WaitingItems.Length;
        var stopped = state.StoppedItems.Length;
        var paused = state.PausedItems.Length;
        var complete = state.CompleteItems.Length;
        var failed = state.FailedItems.Length;
        var skipped = state.SkippedItems.Length;
        var interrupted = state.InterruptedItems.Length;
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