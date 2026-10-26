using System.Text;

namespace AriaSharp.App;

internal static class HelpContent
{
    public static void Render(StringBuilder sb, int width)
    {
        ArgumentNullException.ThrowIfNull(sb);
        var w = Math.Max(40, width - 4);
        sb.AppendLine("┌" + new string('─', w) + "┐");
        sb.AppendLine("│" + Center("HELP", w) + "│");
        sb.AppendLine("├" + new string('─', w) + "┤");
        sb.AppendLine("│" + "  -h, --help            Show this help".PadRight(w) + "│");
        sb.AppendLine("│" + "  -V, --version         Show version".PadRight(w) + "│");
        sb.AppendLine("│" + "  -i, --input <url>     Add URL(s) to download".PadRight(w) + "│");
        sb.Append("│" + "  -f, --file <path>     Add URLs from a list file".PadRight(w) + "│").AppendLine();
        sb.AppendLine("│" + "  -d, --dir <path>      Output directory".PadRight(w) + "│");
        sb.AppendLine("│" + "  -s, --session <path>  Session file path".PadRight(w) + "│");
        sb.AppendLine("│" + "  -c, --config <path>   Config file path".PadRight(w) + "│");
        sb.AppendLine("│" + "  -a, --aria2-path <p>  Path to aria2c binary".PadRight(w) + "│");
        sb.AppendLine("│" + "  -o, --aria2-opt <k=v> aria2 option".PadRight(w) + "│");
        sb.AppendLine("│" + "  -l, --speed-limit <v> Max overall download limit".PadRight(w) + "│");
        sb.AppendLine("│" + "  -n, --no-color        Disable color output".PadRight(w) + "│");
        sb.AppendLine("│" + "  -p, --plain           Plain text output".PadRight(w) + "│");
        sb.AppendLine("│" + "  -e, --inline          Inline dashboard".PadRight(w) + "│");
        sb.AppendLine("│" + "  -P, --no-pager        Disable pager".PadRight(w) + "│");
        sb.AppendLine("│" + "  -v, --verbose         Verbose output".PadRight(w) + "│");
        sb.AppendLine("│" + "  -y, --yes             Assume yes to prompts".PadRight(w) + "│");
        sb.AppendLine("│" + "  -F, --force           Overwrite existing files".PadRight(w) + "│");
        sb.AppendLine("│" + "  -r, --resume          Resume incomplete downloads".PadRight(w) + "│");
        sb.AppendLine("│" + "  -k, --keep-list-files Keep list files".PadRight(w) + "│");
        sb.AppendLine("│" + "  --probe-sizes        Probe sizes for magnet links".PadRight(w) + "│");
        sb.AppendLine("│" + "  --install-deps       Install prerequisites".PadRight(w) + "│");
        sb.AppendLine("│" + "  --install-aria2      Install aria2".PadRight(w) + "│");
        sb.AppendLine("└" + new string('─', w) + "┘");
    }

    private static string Center(string text, int w)
    {
        if (text.Length >= w) return text[..w];
        var left = (w - text.Length) / 2;
        return new string(' ', left) + text + new string(' ', w - left - text.Length);
    }
}
