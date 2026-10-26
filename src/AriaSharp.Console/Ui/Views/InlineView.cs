using System.Text;

using Spectre.Console;

using AriaSharp.Models;

namespace AriaSharp.App;

/// <summary>
/// Single-line status for --inline: no alternate screen, no Figlet.
/// </summary>
internal static class InlineView
{
    public static void WriteProgress(TextWriter writer, SessionState state, TimeSpan elapsed, bool color)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(state);

        var name = "";
        if (state.ActiveItems.Count > 0)
        {
            var item = state.ActiveItems[0];
            name = string.IsNullOrEmpty(item.Name) ? item.Gid : item.Name;
            name = FormatHelpers.Truncate(name, 24);
        }

        var pct = state.OverallPercent;
        var speed = FormatHelpers.FormatSpeed(state.TotalDownloadSpeed);
        var done = FormatHelpers.FormatBytes(state.CompletedLength);
        var total = state.TotalLength > 0 ? FormatHelpers.FormatBytes(state.TotalLength) : "?";
        var eta = state.OverallEta.Format();
        var elapsedText = elapsed.TotalHours >= 1
            ? $"{(int)elapsed.TotalHours}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}"
            : $"{(int)elapsed.TotalMinutes}:{elapsed.Seconds:D2}";

        var line = string.IsNullOrEmpty(name)
            ? $"[{elapsedText}] {pct,5:F1}% {done}/{total} {speed} eta {eta} a={state.NumActive} c={state.CompleteItems.Count} f={state.FailedItems.Count}"
            : $"[{elapsedText}] {pct,5:F1}% {done}/{total} {speed} eta {eta} {name} a={state.NumActive} c={state.CompleteItems.Count} f={state.FailedItems.Count}";

        if (color && !System.Console.IsOutputRedirected)
        {
            var markup =
                $"[grey][[{elapsedText}]][/] [#BB86FC]{pct,5:F1}%[/] [grey]{Markup.Escape(done)}/{Markup.Escape(total)}[/] " +
                $"[#CE93D8]{Markup.Escape(speed)}[/] eta [grey]{Markup.Escape(eta)}[/]";
            if (!string.IsNullOrEmpty(name))
                markup += $" [white]{Markup.Escape(name)}[/]";
            markup += $" [dim]a={state.NumActive} c={state.CompleteItems.Count} f={state.FailedItems.Count}[/]";
            try
            {
                AnsiConsole.Markup("\r" + markup + new string(' ', 8));
            }
            catch (InvalidOperationException)
            {
                writer.Write('\r');
                writer.Write(line);
            }
        }
        else
        {
            writer.Write('\r');
            writer.Write(line);
            if (line.Length < 100)
                writer.Write(new string(' ', Math.Max(0, 100 - line.Length)));
        }
    }

    public static void EndProgress(TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteLine();
    }

    public static void Render(StringBuilder sb, SessionState state, int width)
    {
        ArgumentNullException.ThrowIfNull(sb);
        ArgumentNullException.ThrowIfNull(state);
        var w = Math.Max(40, width - 4);
        sb.AppendLine("┌" + new string('─', w) + "┐");
        sb.AppendLine("│" + Center("INLINE", w) + "│");
        sb.AppendLine("├" + new string('─', w) + "┤");
        var items = state.ActiveItems.Count > 0 ? state.ActiveItems : state.AllItems;
        if (items.Count == 0)
            sb.AppendLine("│" + Center("(idle)", w) + "│");
        else
        {
            foreach (var item in items.Take(8))
            {
                var pct = item.TotalLength > 0 ? (double)item.CompletedLength / item.TotalLength * 100 : 0;
                var line =
                    $" {FormatHelpers.Truncate(item.Name, Math.Min(28, w - 20))} {pct,5:F1}% {FormatHelpers.FormatSpeed(item.DownloadSpeed)}";
                sb.AppendLine("│" + line.PadRight(w)[..w] + "│");
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
