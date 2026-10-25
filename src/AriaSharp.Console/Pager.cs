using System.Text;

namespace AriaSharp.Console;

public static class Pager
{
    public static void Show(StringBuilder sb, int width, int height, int pageSize = 20)
    {
        if (sb.Length == 0) return;
        var lines = sb.ToString().Split('\n');
        var page = 0;
        var total = (int)Math.Ceiling((double)lines.Length / pageSize);
        while (true)
        {
            System.Console.Clear();
            var start = page * pageSize;
            for (var i = start; i < Math.Min(start + pageSize, lines.Length); i++) System.Console.WriteLine(lines[i]);
            System.Console.WriteLine();
            System.Console.Write($"[page {page + 1}/{total}]  q=quit  j/k=down/up  space=next  b=prev  > ");
            var key = System.Console.ReadKey(intercept: true);
            switch (key.Key)
            {
                case System.ConsoleKey.Q: return;
                case System.ConsoleKey.Spacebar: page = Math.Min(total - 1, page + 1); break;
                case System.ConsoleKey.K or System.ConsoleKey.UpArrow: page = Math.Max(0, page - 1); break;
                case System.ConsoleKey.B: page = Math.Max(0, page - 1); break;
                case System.ConsoleKey.G: page = total - 1; break;
                case System.ConsoleKey.H: page = 0; break;
            }
        }
    }
}