

namespace AriaSharp.App;

/// <summary>
/// Plain (no escape codes) progress and status rendering for --plain / non-TUI runs.
/// </summary>
internal static class PlainView
{
    /// <summary>
    /// Single-line live progress. Uses CR to overwrite when the terminal is interactive;
    /// when redirected, emits a new line each tick so logs stay readable.
    /// </summary>
    public static void WriteProgress(TextWriter writer, SessionState state, TimeSpan elapsed, bool interactive)
    {
        ThrowIfNull(writer);
        ThrowIfNull(state);

        var active = state.NumActive;
        var waiting = state.NumWaiting;
        var complete = state.CompleteItems.Count;
        var failed = state.FailedItems.Count;
        var pct = state.OverallPercent;
        var speed = FormatSpeed(state.TotalDownloadSpeed);
        var done = FormatBytes(state.CompletedLength);
        var total = state.TotalLength > 0 ? FormatBytes(state.TotalLength) : "?";
        var eta = state.OverallEta.Format();
        var elapsedText = elapsed.TotalHours >= 1
            ? $"{(int)elapsed.TotalHours}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}"
            : $"{(int)elapsed.TotalMinutes}:{elapsed.Seconds:D2}";

        var name = "";
        if (state.ActiveItems.Count > 0)
        {
            var item = state.ActiveItems[0];
            name = string.IsNullOrEmpty(item.Name) ? item.Gid : item.Name;
            name = Truncate(name, 32);
        }

        var line = string.IsNullOrEmpty(name)
            ? $"[{elapsedText}] {pct,5:F1}%  {done}/{total}  {speed}  eta {eta}  a={active} w={waiting} c={complete} f={failed}"
            : $"[{elapsedText}] {pct,5:F1}%  {done}/{total}  {speed}  eta {eta}  {name}  a={active} w={waiting} c={complete} f={failed}";

        if (interactive)
        {
            writer.Write('\r');
            writer.Write(line);
            if (line.Length < 100)
                writer.Write(new string(' ', 100 - line.Length));
        }
        else
        {
            writer.WriteLine(line);
        }
    }

    public static void EndProgress(TextWriter writer, bool interactive)
    {
        ThrowIfNull(writer);
        if (interactive)
            writer.WriteLine();
    }

    public static void Render(StringBuilder sb, SessionState state, int width)
    {
        ThrowIfNull(sb);
        ThrowIfNull(state);
        var w = Math.Max(40, width - 4);
        sb.AppendLine("┌" + new string('─', w) + "┐");
        sb.AppendLine("│" + Center("PLAIN VIEW", w) + "│");
        sb.AppendLine("├" + new string('─', w) + "┤");
        var line =
            $"  total={state.AllItems.Count} active={state.ActiveItems.Count} waiting={state.WaitingItems.Count} complete={state.CompleteItems.Count} failed={state.FailedItems.Count}";
        sb.AppendLine("│" + line.PadRight(w) + "│");
        sb.AppendLine("│" +
            $"  speed={FormatSpeed(state.TotalDownloadSpeed)} overall={state.OverallPercent:F1}% eta={state.OverallEta.Format()}"
                .PadRight(w) + "│");
        sb.AppendLine("└" + new string('─', w) + "┘");
    }

    private static string Center(string text, int w)
    {
        if (text.Length >= w) return text[..w];
        var left = (w - text.Length) / 2;
        return new string(' ', left) + text + new string(' ', w - left - text.Length);
    }
}
