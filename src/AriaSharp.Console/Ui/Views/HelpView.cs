using Spectre.Console;

namespace AriaSharp.App;

/// <summary>
/// Spectre purple-themed help screen. Falls back to plain
/// <see cref="HelpDocument"/> when colour is disabled.
/// </summary>
internal static class HelpView
{
    public static void Render(AppSettings settings)
    {
        if (!Theme.Enabled || settings.NoColor || settings.Ascii || settings.Plain
            || AnsiConsole.Profile.Capabilities.ColorSystem == ColorSystem.NoColors)
        {
            AnsiConsole.WriteLine(HelpDocument.Render(settings));
            return;
        }

        var purple = new Color(155, 89, 182);
        var purpleBright = new Color(187, 134, 252);
        var purpleDeep = new Color(108, 52, 131);

        AnsiConsole.Write(new FigletText("ariasharp")
            .Color(purpleBright)
            .Centered());
        AnsiConsole.MarkupLine($"[{Theme.Accent}]aria2 client for .NET — purple-gradient dashboard[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.Write(new Panel(
                Align.Left(new Markup(Markup.Escape(
                    "ariasharp [OPTIONS] <url>...\n-i/--input is repeatable; stdin and list files accepted"))))
            .Header("[#E040FB]USAGE[/]")
            .BorderColor(purple)
            .Padding(1, 0));

        foreach (var category in OptionTable.Categories)
        {
            var options = OptionTable.All.Where(o => o.Category == category).ToArray();
            if (options.Length == 0) continue;

            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(purpleDeep)
                .HideHeaders()
                .Expand();
            table.AddColumn(new TableColumn("key").Width(30));
            table.AddColumn("help");

            foreach (var o in options)
            {
                var key = FormatKey(o);
                table.AddRow(
                    new Markup($"[#BB86FC]{Markup.Escape(key)}[/]"),
                    new Markup($"[grey]{Markup.Escape(o.Help)}[/]"));
            }

            AnsiConsole.Write(new Panel(table)
                .Header($"[#E040FB]{Markup.Escape(category.ToUpperInvariant())}[/]")
                .BorderColor(purple)
                .Padding(1, 0));
        }

        AnsiConsole.Write(new Panel(new Markup("[grey]0 ok · 1 failure · 2 usage · 130 interrupted[/]"))
            .Header("[#E040FB]EXIT CODES[/]")
            .BorderColor(purple)
            .Padding(1, 0));

        AnsiConsole.Write(new Panel(new Markup("[grey]defaults < config file < environment < CLI[/]"))
            .Header("[#E040FB]CONFIG[/]")
            .BorderColor(purple)
            .Padding(1, 0));
    }

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
}
