using Spectre.Console;
using Spectre.Console.Rendering;

using AriaSharp.Models;

namespace AriaSharp.App;

/// <summary>
/// Full-screen purple Spectre dashboard for live download progress.
/// </summary>
internal static class DashboardView
{
    private static readonly Color Purple = new(155, 89, 182);
    private static readonly Color PurpleBright = new(187, 134, 252);
    private static readonly Color PurpleDeep = new(108, 52, 131);

    public static IRenderable Build(SessionState state, TimeSpan elapsed)
    {
        ArgumentNullException.ThrowIfNull(state);

        var root = new Grid();
        root.AddColumn();

        root.AddRow(new FigletText("ariasharp")
            .Color(PurpleBright)
            .Centered());

        root.AddRow(new Markup($"[{Theme.Accent}]download dashboard[/]").Centered());
        root.AddRow(new Text(""));

        var barWidth = Math.Min(50, Math.Max(16, System.Console.WindowWidth - 24));
        var filled = (int)Math.Round(Math.Clamp(state.OverallPercent / 100.0, 0, 1) * barWidth);
        var bar =
            $"[#BB86FC]{new string('█', filled)}[/]" +
            $"[#6C3483]{new string('░', barWidth - filled)}[/]";

        var overallRows = new Rows(
            new Markup(bar),
            new Markup(
                $"[grey]{Markup.Escape(FormatHelpers.FormatBytes(state.CompletedLength))}[/] / " +
                $"[grey]{Markup.Escape(state.TotalLength > 0 ? FormatHelpers.FormatBytes(state.TotalLength) : "?")}[/]  " +
                $"[#BB86FC]{state.OverallPercent:F1}%[/]  " +
                $"[grey]{Markup.Escape(FormatHelpers.FormatSpeed(state.TotalDownloadSpeed))}[/]  " +
                $"eta [grey]{Markup.Escape(state.OverallEta.Format())}[/]  " +
                $"[dim]{FormatElapsed(elapsed)}[/]"));

        root.AddRow(new Panel(overallRows)
            .Header("[#E040FB]OVERALL[/]")
            .BorderColor(Purple)
            .Padding(1, 0)
            .Expand());

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(PurpleDeep)
            .Expand();
        table.AddColumn(new TableColumn("[#BB86FC]Name[/]").Width(28));
        table.AddColumn(new TableColumn("[#BB86FC]Progress[/]").Width(10).RightAligned());
        table.AddColumn(new TableColumn("[#BB86FC]Speed[/]").Width(12).RightAligned());
        table.AddColumn(new TableColumn("[#BB86FC]Conn[/]").Width(6).RightAligned());

        if (state.ActiveItems.Count == 0)
        {
            table.AddRow(new Markup("[grey](none)[/]"), new Text(""), new Text(""), new Text(""));
        }
        else
        {
            foreach (var item in state.ActiveItems.Take(12))
            {
                var name = string.IsNullOrEmpty(item.Name) ? item.Gid : item.Name;
                var itemPct = item.TotalLength > 0
                    ? (double)item.CompletedLength / item.TotalLength * 100
                    : 0;
                table.AddRow(
                    new Markup($"[white]{Markup.Escape(FormatHelpers.Truncate(name, 28))}[/]"),
                    new Markup($"[#CE93D8]{itemPct:F1}%[/]"),
                    new Markup($"[grey]{Markup.Escape(FormatHelpers.FormatSpeed(item.DownloadSpeed))}[/]"),
                    new Markup($"[dim]{item.Connections}[/]"));
            }
        }

        root.AddRow(new Panel(table)
            .Header($"[#E040FB]ACTIVE ({state.NumActive})[/]")
            .BorderColor(Purple)
            .Padding(1, 0)
            .Expand());

        var status =
            $"[grey]waiting[/] {state.NumWaiting}  " +
            $"[green]complete[/] {state.CompleteItems.Count}  " +
            $"[red]failed[/] {state.FailedItems.Count}  " +
            $"[yellow]paused[/] {state.NumPaused}";
        root.AddRow(new Panel(new Markup(status))
            .Header("[#E040FB]STATUS[/]")
            .BorderColor(Purple)
            .Padding(1, 0)
            .Expand());

        if (state.FailedItems.Count > 0)
        {
            var failLines = new List<IRenderable>();
            foreach (var item in state.FailedItems.Take(5))
            {
                var name = string.IsNullOrEmpty(item.Name) ? item.Gid : item.Name;
                var err = item.ErrorMessage
                    ?? (item.ErrorCode != 0 ? ErrorCatalog.TextFor(item.ErrorCode) : "error");
                failLines.Add(new Markup(
                    $"[red]•[/] [white]{Markup.Escape(FormatHelpers.Truncate(name, 24))}[/] " +
                    $"[grey]{Markup.Escape(FormatHelpers.Truncate(err, 40))}[/]"));
            }
            root.AddRow(new Panel(new Rows(failLines))
                .Header("[red]FAILED[/]")
                .BorderColor(Color.Red)
                .Padding(1, 0)
                .Expand());
        }

        return root;
    }

    private static string FormatElapsed(TimeSpan ts) =>
        ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}"
            : $"{(int)ts.TotalMinutes}:{ts.Seconds:D2}";
}
