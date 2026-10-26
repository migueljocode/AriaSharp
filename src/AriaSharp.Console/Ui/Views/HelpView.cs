using System.Text;

using Spectre.Console;
using Spectre.Console.Rendering;

namespace AriaSharp.App;

/// <summary>
/// Purple-gradient help: Spectre layout captured as ANSI lines, then paged.
/// </summary>
internal static class HelpView
{
    private static readonly Color Purple = new(155, 89, 182);
    private static readonly Color PurpleBright = new(187, 134, 252);
    private static readonly Color PurpleDeep = new(108, 52, 131);

    public static void Render(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var useColor = Theme.Enabled
            && !settings.NoColor
            && !settings.Ascii
            && !settings.Plain;

        if (!useColor)
        {
            HelpPager.Show(HelpDocument.Render(settings), noPager: settings.NoPager || settings.Plain);
            return;
        }

        var width = SafeWidth();
        var ansi = RenderAnsi(settings, width);
        HelpPager.Show(ansi, noPager: settings.NoPager, preserveAnsi: true);
    }

    /// <summary>Build the full coloured help as an ANSI string for the pager.</summary>
    internal static string RenderAnsi(AppSettings settings, int width)
    {
        var sw = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.Yes,
            ColorSystem = ColorSystemSupport.TrueColor,
            Interactive = InteractionSupport.No,
            Out = new AnsiConsoleOutput(sw),
        });
        console.Profile.Width = Math.Max(40, width);

        console.Write(new FigletText("ariasharp")
            .Color(PurpleBright)
            .Centered());

        console.Write(Align.Center(new Markup($"[{Theme.Accent}]aria2 client for .NET[/] — [{Theme.Magenta}]purple-gradient dashboard[/]")));
        console.WriteLine();

        console.Write(SectionPanel(
            "USAGE",
            new Markup(
                $"[white]ariasharp[/] [{Theme.Muted}][[OPTIONS]][/] [{Theme.PurpleBright}]<url>...[/]\n" +
                $"[{Theme.Muted}]-i/--input[/] is repeatable · stdin and list files accepted")));

        foreach (var category in OptionTable.Categories)
        {
            var options = OptionTable.All.Where(o => o.Category == category).ToArray();
            if (options.Length == 0) continue;

            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(PurpleDeep)
                .HideHeaders()
                .Expand();
            table.AddColumn(new TableColumn("key").Width(32));
            table.AddColumn("help");

            foreach (var o in options)
            {
                var key = FormatKey(o);
                var def = DefaultHint(o);
                var help = string.IsNullOrEmpty(def)
                    ? $"[{Theme.Muted}]{Markup.Escape(o.Help)}[/]"
                    : $"[{Theme.Muted}]{Markup.Escape(o.Help)}[/] [dim]({Markup.Escape(def)})[/]";
                table.AddRow(
                    new Markup($"[{Theme.PurpleBright}]{Markup.Escape(key)}[/]"),
                    new Markup(help));
            }

            console.Write(SectionPanel(category.ToUpperInvariant(), table));
        }

        console.Write(SectionPanel(
            "EXIT CODES",
            new Markup(
                $"[{Theme.Success}]0[/]  ok    " +
                $"[red]1[/]  failure    " +
                $"[yellow]2[/]  usage    " +
                $"[{Theme.Accent}]130[/]  interrupted")));

        console.Write(SectionPanel(
            "CONFIG",
            new Markup(
                $"[{Theme.Muted}]defaults[/] [dim]<[/] [{Theme.Accent}]config file[/] [dim]<[/] [{Theme.PurpleBright}]environment[/] [dim]<[/] [white]CLI[/]\n" +
                $"[{Theme.Muted}]file[/]  {Markup.Escape(AppPaths.SettingsFile)}")));

        console.Write(SectionPanel(
            "KEYS",
            new Markup(
                $"[{Theme.PurpleBright}]j/k[/] [{Theme.Muted}]scroll[/]   " +
                $"[{Theme.PurpleBright}]space/b[/] [{Theme.Muted}]page[/]   " +
                $"[{Theme.PurpleBright}]g/G[/] [{Theme.Muted}]top/end[/]\n" +
                $"[{Theme.PurpleBright}]/[/] [{Theme.Muted}]search[/]  " +
                $"[{Theme.PurpleBright}]n/N[/] [{Theme.Muted}]next/prev[/]   " +
                $"[{Theme.PurpleBright}]q[/] [{Theme.Muted}]quit[/]")));

        console.Write(new Rule($"[{Theme.Magenta}]·[/]")
            .RuleStyle(new Style(PurpleDeep))
            .Centered());

        return sw.ToString();
    }

    private static Panel SectionPanel(string title, IRenderable body) =>
        new Panel(body)
            .Header($"[{Theme.Magenta}]{Markup.Escape(title)}[/]")
            .BorderColor(Purple)
            .Border(BoxBorder.Rounded)
            .Padding(1, 0)
            .Expand();

    private static string FormatKey(Option o)
    {
        var shortPart = o.ShortName is char c ? $"-{c}, " : "    ";
        var value = o.Kind switch
        {
            OptionKind.Flag => "",
            OptionKind.Integer => " <n>",
            OptionKind.List => " <value>",
            _ => o.ValueName is { } vn ? $" {vn}" : " <value>",
        };
        return $"{shortPart}--{o.LongName}{value}";
    }

    private static string DefaultHint(Option o) => o.LongName switch
    {
        "jobs" => $"default {AppDefaults.MaxConcurrentDownloads}",
        "connections" or "split" or "per-server" => $"default {AppDefaults.MaxConnectionPerServer}",
        "refresh" => $"default {AppDefaults.RefreshIntervalMs}ms",
        "output" => $"default {AppDefaults.OutputDirDefault}",
        "session" => $"default {AppDefaults.SessionFileName}",
        _ => "",
    };

    private static int SafeWidth()
    {
        try
        {
            var w = System.Console.WindowWidth;
            return w > 0 ? w : 100;
        }
        catch (IOException) { return 100; }
        catch (InvalidOperationException) { return 100; }
    }
}
