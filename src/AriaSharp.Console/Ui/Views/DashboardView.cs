using Spectre.Console;
using Spectre.Console.Rendering;

using AriaSharp.Models;

namespace AriaSharp.App;

/// <summary>
/// Full-width purple Spectre dashboard for live download progress.
/// </summary>
internal static class DashboardView
{
    private static readonly Color Purple = new(155, 89, 182);
    private static readonly Color PurpleDeep = new(108, 52, 131);

    public static IRenderable Build(SessionState state, TimeSpan elapsed, int split = 16)
    {
        ArgumentNullException.ThrowIfNull(state);

        var width = SafeWindowWidth();
        var root = new Grid().Expand();
        root.AddColumn(new GridColumn().NoWrap());

        // Compact logo band (no "download dashboard" label)
        root.AddRow(BuildLogo());
        root.AddRow(new Text(""));

        var barWidth = Math.Max(24, width - 8);
        var filled = (int)Math.Round(Math.Clamp(state.OverallPercent / 100.0, 0, 1) * barWidth);
        var bar =
            $"[#BB86FC]{new string('█', filled)}[/]" +
            $"[#6C3483]{new string('░', Math.Max(0, barWidth - filled))}[/]";

        var etaText = state.NumActive == 0 && state.NumPaused > 0 && state.TotalDownloadSpeed <= 0
            ? "paused"
            : state.OverallEta.Format();
        var overallRows = new Rows(
            new Markup(bar),
            new Markup(
                $"[grey]{Markup.Escape(FormatHelpers.FormatBytes(state.CompletedLength))}[/] / " +
                $"[grey]{Markup.Escape(state.TotalLength > 0 ? FormatHelpers.FormatBytes(state.TotalLength) : "?")}[/]  " +
                $"[#BB86FC]{state.OverallPercent:F1}%[/]  " +
                $"[grey]{Markup.Escape(FormatHelpers.FormatSpeed(state.TotalDownloadSpeed))}[/]  " +
                $"eta [grey]{Markup.Escape(etaText)}[/]  " +
                $"[dim]{FormatElapsed(elapsed)}[/]"));

        root.AddRow(new Panel(overallRows)
            .Header("[#E040FB]OVERALL[/]")
            .BorderColor(Purple)
            .Padding(1, 0)
            .Expand());

        // Active table — full width; name takes remaining space
        var nameWidth = Math.Max(24, width - 56);
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(PurpleDeep)
            .Expand();
        table.AddColumn(new TableColumn("[#BB86FC]Name[/]").NoWrap());
        table.AddColumn(new TableColumn("[#BB86FC]Progress[/]").Width(10).RightAligned());
        table.AddColumn(new TableColumn("[#BB86FC]Speed[/]").Width(11).RightAligned());
        table.AddColumn(new TableColumn("[#BB86FC]ETA[/]").Width(9).RightAligned());
        table.AddColumn(new TableColumn("[#BB86FC]Connections[/]").Width(12).RightAligned());
        table.AddColumn(new TableColumn("[#BB86FC]Split[/]").Width(7).RightAligned());

        if (state.ActiveItems.Count == 0)
        {
            table.AddRow(
                new Markup("[grey](none)[/]"),
                new Text(""), new Text(""), new Text(""), new Text(""), new Text(""));
        }
        else
        {
            foreach (var item in state.ActiveItems.Take(12))
            {
                var name = string.IsNullOrEmpty(item.Name) ? item.Gid : item.Name;
                var itemPct = item.TotalLength > 0
                    ? (double)item.CompletedLength / item.TotalLength * 100
                    : 0;
                var eta = ItemEta(item);
                table.AddRow(
                    new Markup($"[white]{Markup.Escape(FormatHelpers.Truncate(name, nameWidth))}[/]"),
                    new Markup($"[#CE93D8]{itemPct:F1}%[/]"),
                    new Markup($"[grey]{Markup.Escape(FormatHelpers.FormatSpeed(item.DownloadSpeed))}[/]"),
                    new Markup($"[grey]{Markup.Escape(eta)}[/]"),
                    new Markup($"[dim]{item.Connections}[/]"),
                    new Markup($"[dim]{split}[/]"));
            }
        }

        root.AddRow(new Panel(table)
            .Header($"[#E040FB]ACTIVE ({state.NumActive})[/]")
            .BorderColor(Purple)
            .Padding(1, 0)
            .Expand());

        var all = state.AllItems.Count;
        if (all == 0)
            all = state.ActiveItems.Count + state.WaitingItems.Count + state.CompleteItems.Count
                + state.FailedItems.Count + state.PausedItems.Count + state.SkippedItems.Count
                + state.StoppedItems.Count + state.InterruptedItems.Count;

        var status =
            $"[white]All[/] {all}  " +
            $"[#BB86FC]Active[/] {state.NumActive}  " +
            $"[green]Completed[/] {state.CompleteItems.Count}  " +
            $"[red]Failed[/] {state.FailedItems.Count}  " +
            $"[grey]Waiting[/] {state.NumWaiting}  " +
            $"[yellow]Paused[/] {state.NumPaused}  " +
            $"[dim]Skipped[/] {state.SkippedItems.Count}";
        root.AddRow(new Panel(new Markup(status))
            .Header("[#E040FB]STATUS[/]")
            .BorderColor(Purple)
            .Padding(1, 0)
            .Expand());

        if (state.CompleteItems.Count > 0)
        {
            var lines = state.CompleteItems.Take(6).Select(i =>
            {
                var name = string.IsNullOrEmpty(i.Name) ? i.Gid : i.Name;
                var size = FormatHelpers.FormatBytes(i.CompletedLength > 0 ? i.CompletedLength : i.TotalLength);
                return new Markup(
                    $"[dim]{Markup.Escape(size.PadLeft(10))}[/]  [green]✓[/] [white]{Markup.Escape(FormatHelpers.Truncate(name, Math.Max(20, width - 20)))}[/]");
            });
            root.AddRow(new Panel(new Rows(lines))
                .Header($"[green]COMPLETE ({state.CompleteItems.Count})[/]")
                .BorderColor(Color.Green)
                .Padding(1, 0)
                .Expand());
        }

        if (state.FailedItems.Count > 0)
        {
            var lines = state.FailedItems.Take(6).Select(i =>
            {
                var name = string.IsNullOrEmpty(i.Name) ? i.Gid : i.Name;
                var err = i.ErrorMessage
                    ?? (i.ErrorCode != 0 ? ErrorCatalog.TextFor(i.ErrorCode) : "error");
                return new Markup(
                    $"[red]✗[/] [white]{Markup.Escape(FormatHelpers.Truncate(name, Math.Max(16, width / 2)))}[/]  "
                    + $"[grey]{Markup.Escape(FormatHelpers.Truncate(err, Math.Max(16, width / 3)))}[/]");
            });
            root.AddRow(new Panel(new Rows(lines))
                .Header($"[red]FAILED ({state.FailedItems.Count})[/]")
                .BorderColor(Color.Red)
                .Padding(1, 0)
                .Expand());
        }

        if (state.SkippedItems.Count > 0)
        {
            var lines = state.SkippedItems.Take(4).Select(i =>
            {
                var name = string.IsNullOrEmpty(i.Name) ? i.Gid : i.Name;
                return new Markup(
                    $"[dim]·[/] [grey]{Markup.Escape(FormatHelpers.Truncate(name, Math.Max(20, width - 12)))}[/]  [dim]already exists[/]");
            });
            root.AddRow(new Panel(new Rows(lines))
                .Header($"[dim]SKIPPED ({state.SkippedItems.Count})[/]")
                .BorderColor(Color.Grey)
                .Padding(1, 0)
                .Expand());
        }

        // Purple ribbon keybinding strip (help-page style)
        root.AddRow(new Panel(new Markup(
                $"[#BB86FC on #6C3483] P [/][white on #6C3483] pause [/]  "
                + $"[#BB86FC on #6C3483] R [/][white on #6C3483] resume [/]  "
                + $"[#BB86FC on #6C3483] Q [/][white on #6C3483]/[/][#BB86FC on #6C3483] Esc [/][white on #6C3483] stop [/]"))
            .Header($"[{Theme.Magenta}]KEYS[/]")
            .BorderColor(Purple)
            .Border(BoxBorder.Rounded)
            .Padding(1, 0)
            .Expand());

        return root;
    }

    private static Align BuildLogo()
    {
        // Stylized mark — compact, full-width friendly
        var art = new Markup(
            $"[#BB86FC] █████╗ ██████╗ ██╗ █████╗ [/][#E040FB]███████╗██╗  ██╗ █████╗ ██████╗ ██████╗ [/]\n" +
            $"[#BB86FC]██╔══██╗██╔══██╗██║██╔══██╗[/][#CE93D8]██╔════╝██║  ██║██╔══██╗██╔══██╗██╔══██╗[/]\n" +
            $"[#BB86FC]███████║██████╔╝██║███████║[/][#E040FB]███████╗███████║███████║██████╔╝██████╔╝[/]\n" +
            $"[#BB86FC]██╔══██║██╔══██╗██║██╔══██║[/][#CE93D8]╚════██║██╔══██║██╔══██║██╔══██╗██╔═══╝ [/]\n" +
            $"[#BB86FC]██║  ██║██║  ██║██║██║  ██║[/][#E040FB]███████║██║  ██║██║  ██║██║  ██║██║     [/]\n" +
            $"[#BB86FC]╚═╝  ╚═╝╚═╝  ╚═╝╚═╝╚═╝  ╚═╝[/][#CE93D8]╚══════╝╚═╝  ╚═╝╚═╝  ╚═╝╚═╝  ╚═╝╚═╝     [/]");
        return Align.Center(art);
    }

    private static string ItemEta(DownloadItem item)
    {
        if (item.State == DownloadState.Paused)
            return "paused";
        if (item.DownloadSpeed <= 0 || item.TotalLength <= 0 || item.CompletedLength >= item.TotalLength)
            return "--";
        var remain = item.TotalLength - item.CompletedLength;
        var secs = remain / (double)item.DownloadSpeed;
        if (secs > 86400) return ">1d";
        var ts = TimeSpan.FromSeconds(secs);
        return ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}"
            : $"{(int)ts.TotalMinutes}:{ts.Seconds:D2}";
    }

    private static string FormatElapsed(TimeSpan ts) =>
        ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}"
            : $"{(int)ts.TotalMinutes}:{ts.Seconds:D2}";

    private static int SafeWindowWidth()
    {
        try
        {
            var w = System.Console.WindowWidth;
            return w > 0 ? w : 80;
        }
        catch (IOException) { return 80; }
        catch (InvalidOperationException) { return 80; }
    }
}
