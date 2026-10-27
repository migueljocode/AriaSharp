using System.Text.RegularExpressions;

namespace AriaSharp.App;

/// <summary>
/// less-style pager: alternate screen, scroll keys, purple status line.
/// Supports ANSI content from Spectre (preserveAnsi) or plain HelpDocument text.
/// </summary>
internal static partial class HelpPager
{
    public static void Show(string document, bool noPager, bool preserveAnsi = false)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (noPager
            || System.Console.IsOutputRedirected
            || System.Console.IsInputRedirected
            || !IsInteractive())
        {
            System.Console.Out.Write(document);
            if (!document.EndsWith('\n'))
                System.Console.Out.WriteLine();
            return;
        }

        var width = SafeWidth();
        var height = SafeHeight();
        var lines = preserveAnsi
            ? SplitAnsiLines(document)
            : Wrap(document, Math.Max(40, width));
        if (lines.Count == 0)
            return;

        var bodyRows = Math.Max(1, height - 1);
        if (lines.Count <= bodyRows)
        {
            foreach (var line in lines)
                System.Console.Out.WriteLine(line);
            return;
        }

        using var guard = ScreenGuard.TryEnter();
        var offset = 0;
        var search = "";
        try
        {
            while (true)
            {
                width = SafeWidth();
                height = SafeHeight();
                bodyRows = Math.Max(1, height - 1);
                if (!preserveAnsi)
                    lines = Wrap(document, Math.Max(40, width));
                offset = Math.Clamp(offset, 0, Math.Max(0, lines.Count - bodyRows));

                DrawPage(lines, offset, bodyRows, width);
                DrawStatus(offset, bodyRows, lines.Count, width, search);

                var key = System.Console.ReadKey(intercept: true);
                if (key.Key is ConsoleKey.Q or ConsoleKey.Escape)
                    break;
                if (key.Key is ConsoleKey.DownArrow || key.KeyChar == 'j')
                    offset = Math.Min(offset + 1, Math.Max(0, lines.Count - bodyRows));
                else if (key.Key is ConsoleKey.UpArrow || key.KeyChar == 'k')
                    offset = Math.Max(0, offset - 1);
                else if (key.Key is ConsoleKey.PageDown || key.Key == ConsoleKey.Spacebar || key.KeyChar == 'f')
                    offset = Math.Min(offset + bodyRows, Math.Max(0, lines.Count - bodyRows));
                else if (key.Key is ConsoleKey.PageUp || key.KeyChar == 'b')
                    offset = Math.Max(0, offset - bodyRows);
                else if (key.KeyChar == 'g' || key.Key == ConsoleKey.Home)
                    offset = 0;
                else if (key.KeyChar == 'G' || key.Key == ConsoleKey.End)
                    offset = Math.Max(0, lines.Count - bodyRows);
                else if (key.KeyChar == '/')
                {
                    search = ReadSearch(width);
                    if (!string.IsNullOrEmpty(search))
                        offset = FindNext(lines, search, offset + 1, forward: true);
                }
                else if (key.KeyChar == 'n' && !string.IsNullOrEmpty(search))
                    offset = FindNext(lines, search, offset + 1, forward: true);
                else if (key.KeyChar == 'N' && !string.IsNullOrEmpty(search))
                    offset = FindNext(lines, search, offset - 1, forward: false);
            }
        }
        catch (InvalidOperationException) { }
        catch (IOException) { }
    }

    private static void DrawPage(List<string> lines, int offset, int bodyRows, int width)
    {
        System.Console.Write("\x1b[H\x1b[J");
        var end = Math.Min(lines.Count, offset + bodyRows);
        for (var i = offset; i < end; i++)
        {
            System.Console.Write(lines[i]);
            // reset attributes so status/next line stays clean
            System.Console.Write("\x1b[0m\x1b[K\n");
        }
        for (var i = end - offset; i < bodyRows; i++)
            System.Console.Write("\x1b[K\n");
    }

    private static void DrawStatus(int offset, int bodyRows, int total, int width, string search)
    {
        var end = Math.Min(total, offset + bodyRows);
        var pct = total <= bodyRows ? 100 : (int)(100.0 * end / total);
        var label = end >= total ? "END" : $"{pct}%";
        var msg = string.IsNullOrEmpty(search)
            ? $"  ✦ Help  {offset + 1}–{end}/{total}  {label}  ·  j/k  space/b  g/G  /  q  "
            : $"  ✦ Help  /{search}  {offset + 1}–{end}/{total}  {label}  ·  n/N  q  ";
        if (VisibleLength(msg) > width)
            msg = TrimVisible(msg, width);
        // purple reverse-ish status (bright white on deep purple)
        System.Console.Write($"\x1b[48;2;108;52;131m\x1b[38;2;255;255;255m{msg.PadRight(Math.Max(0, width))}\x1b[0m");
    }

    private static string ReadSearch(int width)
    {
        System.Console.Write("\x1b[48;2;108;52;131m\x1b[38;2;187;134;252m/");
        var sb = new StringBuilder();
        while (true)
        {
            var key = System.Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Escape) { System.Console.Write("\x1b[0m"); return ""; }
            if (key.Key == ConsoleKey.Backspace && sb.Length > 0)
            {
                sb.Length--;
                System.Console.Write("\b \b");
            }
            else if (!char.IsControl(key.KeyChar))
            {
                sb.Append(key.KeyChar);
                System.Console.Write(key.KeyChar);
            }
        }
        System.Console.Write("\x1b[0m");
        return sb.ToString();
    }

    private static int FindNext(List<string> lines, string query, int start, bool forward)
    {
        static bool Match(string line, string q) =>
            StripAnsi(line).Contains(q, StringComparison.OrdinalIgnoreCase);

        if (forward)
        {
            for (var i = Math.Max(0, start); i < lines.Count; i++)
                if (Match(lines[i], query)) return i;
            for (var i = 0; i < Math.Min(start, lines.Count); i++)
                if (Match(lines[i], query)) return i;
        }
        else
        {
            for (var i = Math.Min(lines.Count - 1, start); i >= 0; i--)
                if (Match(lines[i], query)) return i;
            for (var i = lines.Count - 1; i > start; i--)
                if (Match(lines[i], query)) return i;
        }
        return Math.Clamp(start, 0, Math.Max(0, lines.Count - 1));
    }

    internal static List<string> Wrap(string document, int width)
    {
        var result = new List<string>();
        using var reader = new StringReader(document);
        string? raw;
        while ((raw = reader.ReadLine()) is not null)
        {
            if (raw.Length <= width)
            {
                result.Add(raw);
                continue;
            }
            var rest = raw;
            while (rest.Length > width)
            {
                var breakAt = rest.LastIndexOf(' ', width);
                if (breakAt < width / 2) breakAt = width;
                result.Add(rest[..breakAt].TrimEnd());
                rest = rest[breakAt..].TrimStart();
            }
            if (rest.Length > 0) result.Add(rest);
        }
        return result;
    }

    internal static List<string> SplitAnsiLines(string document)
    {
        var result = new List<string>();
        using var reader = new StringReader(document);
        string? line;
        while ((line = reader.ReadLine()) is not null)
            result.Add(line);
        return result;
    }

    private static string StripAnsi(string s) => AnsiRegex().Replace(s, "");

    private static int VisibleLength(string s) => StripAnsi(s).Length;

    private static string TrimVisible(string s, int max)
    {
        var plain = StripAnsi(s);
        if (plain.Length <= max) return s;
        return plain[..max];
    }

    [GeneratedRegex(@"\x1B\[[0-9;]*[A-Za-z]")]
    private static partial Regex AnsiRegex();

    private static bool IsInteractive()
    {
        try { return !System.Console.IsOutputRedirected && System.Console.WindowHeight > 0; }
        catch (IOException) { return false; }
        catch (InvalidOperationException) { return false; }
    }

    private static int SafeWidth()
    {
        try { var w = System.Console.WindowWidth; return w > 0 ? w : 80; }
        catch (IOException) { return 80; }
        catch (InvalidOperationException) { return 80; }
    }

    private static int SafeHeight()
    {
        try { var h = System.Console.WindowHeight; return h > 0 ? h : 24; }
        catch (IOException) { return 24; }
        catch (InvalidOperationException) { return 24; }
    }
}
