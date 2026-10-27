
namespace AriaSharp.App;

/// <summary>
/// The <c>--help</c> screen. Every option line is derived from <see cref="OptionTable"/> — the
/// help text and option letters can never drift from the parser. Output is plain text and
/// grep-friendly; a full-screen colour pager is built later on top of <see cref="OptionTable"/>.
/// </summary>
internal static class HelpDocument
{
    public static string Render(AppSettings settings)
    {
        var sb = new StringBuilder(2048);
        var width = GetWidth();

        sb.AppendLine("ariasharp");
        sb.AppendLine("  aria2 client for .NET");
        sb.AppendLine();
        sb.AppendLine("USAGE");
        sb.AppendLine("  ariasharp [OPTIONS] <url>...");
        sb.AppendLine("  -i/--input is repeatable; -i url1 -i url2");
        sb.AppendLine("  a URL, magnet link, session file or an HTTP(S)/FTP(S)/SFTP list file");
        sb.AppendLine("  may be given as a positional argument, via stdin, or from the clipboard when interactive.");
        sb.AppendLine("  File paths are opened and scanned for http(s)/ftp/magnet URLs.");
        sb.AppendLine("  --from-clipboard forces clipboard; --from-file <path> forces list files.");
        sb.AppendLine();

        foreach (var category in OptionTable.Categories)
        {
            var options = OptionTable.All.Where(o => o.Category == category).ToArray();
            if (options.Length == 0) continue;
            sb.AppendLine(category.ToUpperInvariant());
            var keyWidth = 0;
            foreach (var o in options)
                keyWidth = Math.Max(keyWidth, FormatKey(o).Length);
            foreach (var o in options)
            {
                var key = FormatKey(o).PadRight(keyWidth);
                var help = AppendDefault(o, settings);
                sb.AppendLine($"  {key}  {help}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("CONFIGURATION");
        sb.AppendLine($"  Settings file:   {AppPaths.SettingsFile}");
        sb.AppendLine("  Precedence (lowest wins): defaults < config file < environment < CLI.");
        sb.AppendLine($"  Environment: {AppDefaults.EnvPrefix}JOBS / {AppDefaults.EnvPrefix}CONNECTIONS / {AppDefaults.EnvPrefix}LIMIT ...");
        sb.AppendLine("  Also: NO_COLOR, ARIASHARP_NETWORK (online|offline), ARIASHARP_ASCII,");
        sb.AppendLine($"  ARIASHARP_CONFIG, ARIASHARP_MIRROR, standard proxy variables (http_proxy..).");
        sb.AppendLine();
        sb.AppendLine("EXIT CODES");
        sb.AppendLine("  0  ok");
        sb.AppendLine("  1  download or run failed");
        sb.AppendLine("  2  usage error");
        sb.AppendLine("  130  interrupted (ctrl-c / signal)");
        sb.AppendLine();
        sb.AppendLine("KEYS (help pager)");
        sb.AppendLine("  j/k and up/down  scroll,  Space / PgDn / PgUp  page,  g / G  top / bottom,");
        sb.AppendLine("  /  search (n / N  next / prev),  q or Esc  quit.");
        return sb.ToString();
    }

    private static string FormatKey(Option o)
    {
        var sb = new StringBuilder();
        if (o.ShortName.HasValue)
            _ = sb.Append($"-{o.ShortName}  --{o.LongName}");
        else
            _ = sb.Append($"    --{o.LongName}");
        if (o.TakesValue)
        {
            _ = sb.Append(' ').Append(o.ValueName ?? "<value>");
        }
        return sb.ToString();
    }

    private static string AppendDefault(Option o, AppSettings settings)
    {
        var help = o.Help;
        return o.LongName switch
        {
            "jobs" => $"{help} (default: {AppDefaults.MaxConcurrentDownloads})",
            "connections" or "split" or "per-server" => $"{help} (default: {AppDefaults.MaxConnectionPerServer})",
            "refresh" => $"{help} (default: {AppDefaults.RefreshIntervalMs})",
            "output" => $"{help} (default: {AppDefaults.OutputDirDefault})",
            "session" => $"{help} (default: {AppDefaults.SessionFileName})",
            _ => help,
        };
    }

    private static int GetWidth()
    {
        try { return System.Console.WindowWidth > 0 ? System.Console.WindowWidth : 100; }
        catch (System.IO.IOException) { return 100; }
        catch (InvalidOperationException) { return 100; }
    }
}
