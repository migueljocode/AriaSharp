using System.Text;

namespace AriaSharp.App;

/// <summary>
/// less-style pager for the help document: alternate screen, scroll keys, status line.
/// Falls back to writing the full text when not interactive or <c>--no-pager</c>.
/// </summary>
internal static class HelpPager
{
    public static void Show(string document, bool noPager)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (noPager
            || System.Console.IsOutputRedirected
            || System.Console.IsInputRedirected
            || !AnsiConsoleProfileInteractive())
        {
            System.Console.Out.Write(document);
            if (!document.EndsWith('\n'))
                System.Console.Out.WriteLine();
            return;
        }

        var width = SafeWidth();
        var height = SafeHeight();
        var lines = Wrap(document, Math.Max(40, width));
        if (lines.Count == 0)
            return;

        // If content fits, just print once without interactive loop
        var bodyRows = Math.Max(1, height - 1); // reserve status line
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
                // Re-wrap if width changed
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
        catch (InvalidOperationException)
        {
            // console vanished
        }
        catch (IOException)
        {
        }
    }

    private static void DrawPage(List<string> lines, int offset, int bodyRows, int width)
    {
        System.Console.SetCursorPosition(0, 0);
        // clear screen within alt buffer
        System.Console.Write("\x1b[H\x1b[J");
        var end = Math.Min(lines.Count, offset + bodyRows);
        for (var i = offset; i < end; i++)
        {
            var line = lines[i];
            if (line.Length > width)
                line = line[..width];
            System.Console.Write(line);
            System.Console.Write(new string(' ', Math.Max(0, width - line.Length)));
            if (i < end - 1 || end - offset < bodyRows)
                System.Console.WriteLine();
            else
                System.Console.Write('\n');
        }
        // pad remaining body rows
        for (var i = end - offset; i < bodyRows; i++)
            System.Console.WriteLine(new string(' ', width));
    }

    private static void DrawStatus(int offset, int bodyRows, int total, int width, string search)
    {
        var end = Math.Min(total, offset + bodyRows);
        var pct = total <= bodyRows ? 100 : (int)(100.0 * end / total);
        var label = end >= total ? "END" : $"{pct}%";
        var msg = string.IsNullOrEmpty(search)
            ? $" Help  lines {offset + 1}-{end}/{total}  {label}  │ j/k scroll  space/b page  g/G  / search  q quit "
            : $" Help  /{search}  lines {offset + 1}-{end}/{total}  {label}  │ n/N next  q quit ";
        if (msg.Length > width)
            msg = msg[..width];
        // reverse video status
        System.Console.Write($"\x1b[7m{msg.PadRight(width)}\x1b[0m");
    }

    private static string ReadSearch(int width)
    {
        System.Console.Write("\x1b[7m/");
        var sb = new StringBuilder();
        while (true)
        {
            var key = System.Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
                break;
            if (key.Key == ConsoleKey.Escape)
                return "";
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
        if (forward)
        {
            for (var i = Math.Max(0, start); i < lines.Count; i++)
            {
                if (lines[i].Contains(query, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            for (var i = 0; i < Math.Min(start, lines.Count); i++)
            {
                if (lines[i].Contains(query, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
        }
        else
        {
            for (var i = Math.Min(lines.Count - 1, start); i >= 0; i--)
            {
                if (lines[i].Contains(query, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            for (var i = lines.Count - 1; i > start; i--)
            {
                if (lines[i].Contains(query, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
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
                if (breakAt < width / 2)
                    breakAt = width;
                result.Add(rest[..breakAt].TrimEnd());
                rest = rest[breakAt..].TrimStart();
            }
            if (rest.Length > 0)
                result.Add(rest);
        }
        return result;
    }

    private static bool AnsiConsoleProfileInteractive()
    {
        try
        {
            return !System.Console.IsOutputRedirected && System.Console.WindowHeight > 0;
        }
        catch (IOException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static int SafeWidth()
    {
        try
        {
            var w = System.Console.WindowWidth;
            return w > 0 ? w : 80;
        }
        catch (IOException) { return 80; }
        catch (InvalidOperationException) { return 80; }
    }

    private static int SafeHeight()
    {
        try
        {
            var h = System.Console.WindowHeight;
            return h > 0 ? h : 24;
        }
        catch (IOException) { return 24; }
        catch (InvalidOperationException) { return 24; }
    }
}
