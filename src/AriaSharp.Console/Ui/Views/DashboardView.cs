namespace AriaSharp.App;

/// <summary>
/// Full-width purple Spectre dashboard. Build composes small section helpers (SRP).
/// </summary>
internal static class DashboardView
{
    private static readonly Color Purple = new(155, 89, 182);
    private static readonly Color PurpleDeep = new(108, 52, 131);

    public static IRenderable Build(SessionState state, TimeSpan elapsed, int split = 16, int frame = -1, double? shown = null)
    {
        ThrowIfNull(state);
        var width = TerminalMetrics.WindowWidth();
        var root = new Grid().Expand();
        root.AddColumn(new GridColumn().NoWrap());
        BrandLogo.AddTo(root, frame);
        root.AddRow(BuildOverall(state, elapsed, width, frame, shown));
        root.AddRow(BuildActiveTable(state, split, frame, width));
        root.AddRow(BuildStatus(state));
        if (state.CompleteItems is { Count: > 0 })
            root.AddRow(BuildComplete(state, width));
        if (state.FailedItems is { Count: > 0 })
            root.AddRow(BuildFailed(state, width));
        if (state.SkippedItems is { Count: > 0 })
            root.AddRow(BuildSkipped(state, width));
        root.AddRow(BuildKeys());
        return root;
    }

    private static Panel BuildOverall(SessionState state, TimeSpan elapsed, int width, int frame, double? shown)
    {
        var barWidth = Math.Max(24, width - 8);
        var finished = state.NumActive == 0 && state.NumWaiting == 0 && state.NumPaused == 0 && state.CompleteItems.Count > 0;
        var pausedOnly = state.NumActive == 0 && state.NumPaused > 0;
        var ramp = finished ? Gradient.Green : pausedOnly ? Gradient.Amber : Gradient.Violet;
        var barFrame = finished || pausedOnly ? -1 : frame;
        var bar = Gradient.Bar(barWidth, shown ?? state.OverallPercent / 100.0, ramp, barFrame);
        var etaText = state.NumActive == 0 && state.NumPaused > 0 && state.TotalDownloadSpeed <= 0
            ? "paused"
            : state.OverallEta.Format();

        var rows = new Rows(
            new Markup(bar),
            new Markup(
                $"[grey]{Markup.Escape(state.CompletedLength.AsByteSize())}[/] / " +
                $"[grey]{Markup.Escape(state.TotalLength > 0 ? FormatBytes(state.TotalLength) : "?")}[/]  " +
                $"[#BB86FC]{state.OverallPercent:F1}%[/]  " +
                $"[grey]{Markup.Escape(state.TotalDownloadSpeed.AsSpeed())}[/]  " +
                $"eta [grey]{Markup.Escape(etaText)}[/]  " +
                $"[dim]{elapsed.AsElapsed()}[/]"));

        return PurplePanel(rows, "[#E040FB]OVERALL[/]");
    }

    private static Panel BuildActiveTable(SessionState state, int split, int frame, int width)
    {
        var nameWidth = Math.Max(24, width - 62);
        var finished = state.NumActive == 0 && state.NumWaiting == 0 && state.NumPaused == 0 && state.CompleteItems.Count > 0;
        var pausedOnly = state.NumActive == 0 && state.NumPaused > 0;
        var barFrame = finished || pausedOnly ? -1 : frame;

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(PurpleDeep)
            .Expand();
        table.AddColumn(new TableColumn("[#BB86FC]Name[/]").NoWrap());
        table.AddColumn(new TableColumn("[#BB86FC]Progress[/]").Width(16).RightAligned());
        table.AddColumn(new TableColumn("[#BB86FC]Speed[/]").Width(10).RightAligned());
        table.AddColumn(new TableColumn("[#BB86FC]ETA[/]").Width(8).RightAligned());
        table.AddColumn(new TableColumn("[#BB86FC]Conn[/]").Width(6).RightAligned());
        table.AddColumn(new TableColumn("[#BB86FC]Split[/]").Width(5).RightAligned());

        if (state.ActiveItems.Count == 0)
        {
            table.AddRow(new Markup("[grey](none)[/]"), new Text(""), new Text(""), new Text(""), new Text(""), new Text(""));
        }
        else
        {
            foreach (var item in state.ActiveItems.Take(12))
            {
                var name = string.IsNullOrEmpty(item.Name) ? item.Gid : item.Name;
                var itemPct = item.TotalLength > 0 ? (double)item.CompletedLength / item.TotalLength * 100 : 0;
                table.AddRow(
                    new Markup($"[#BB86FC]{(frame < 0 ? '●' : Gradient.Spinner(frame))}[/] [white]{Markup.Escape(Truncate(name, nameWidth - 2))}[/]"),
                    new Markup($"{Gradient.Bar(8, itemPct / 100, Gradient.Violet, barFrame)} [#CE93D8]{itemPct,5:F1}%[/]"),
                    new Markup($"[grey]{Markup.Escape(FormatSpeed(item.DownloadSpeed))}[/]"),
                    new Markup($"[grey]{Markup.Escape(ItemEta(item))}[/]"),
                    new Markup($"[dim]{item.Connections}[/]"),
                    new Markup($"[dim]{split}[/]"));
            }
        }

        return PurplePanel(table, $"[#E040FB]ACTIVE ({state.NumActive})[/]");
    }

    private static Panel BuildStatus(SessionState state)
    {
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
            $"[yellow]❚❚ Paused[/] {state.NumPaused}  " +
            $"[dim]Skipped[/] {state.SkippedItems.Count}";
        return PurplePanel(new Markup(status), "[#E040FB]STATUS[/]");
    }

    private static Panel BuildComplete(SessionState state, int width)
    {
        var lines = state.CompleteItems.Take(6).Select(i =>
        {
            var name = string.IsNullOrEmpty(i.Name) ? i.Gid : i.Name;
            var size = FormatBytes(i.CompletedLength > 0 ? i.CompletedLength : i.TotalLength);
            return new Markup(
                $"[dim]{Markup.Escape(size.PadLeft(10))}[/]  [green]✓[/] [white]{Markup.Escape(Truncate(name, Math.Max(20, width - 20)))}[/]");
        });
        return new Panel(new Rows(lines))
            .Header($"[green]COMPLETE ({state.CompleteItems.Count})[/]")
            .BorderColor(Color.Green)
            .Border(BoxBorder.Rounded)
            .Padding(1, 0)
            .Expand();
    }

    private static Panel BuildFailed(SessionState state, int width)
    {
        var lines = state.FailedItems.Take(6).Select(i =>
        {
            var name = string.IsNullOrEmpty(i.Name) ? i.Gid : i.Name;
            var err = i.ErrorMessage ?? (i.ErrorCode != 0 ? TextFor(i.ErrorCode) : "error");
            return new Markup(
                $"[red]✗[/] [white]{Markup.Escape(Truncate(name, Math.Max(16, width / 2)))}[/]  "
                + $"[grey]{Markup.Escape(Truncate(err, Math.Max(16, width / 3)))}[/]");
        });
        return new Panel(new Rows(lines))
            .Header($"[red]FAILED ({state.FailedItems.Count})[/]")
            .BorderColor(Color.Red)
            .Border(BoxBorder.Rounded)
            .Padding(1, 0)
            .Expand();
    }

    private static Panel BuildSkipped(SessionState state, int width)
    {
        var lines = state.SkippedItems.Take(4).Select(i =>
        {
            var name = string.IsNullOrEmpty(i.Name) ? i.Gid : i.Name;
            return new Markup(
                $"[dim]·[/] [grey]{Markup.Escape(Truncate(name, Math.Max(20, width - 12)))}[/]  [dim]already exists[/]");
        });
        return new Panel(new Rows(lines))
            .Header($"[dim]SKIPPED ({state.SkippedItems.Count})[/]")
            .BorderColor(Color.Grey)
            .Border(BoxBorder.Rounded)
            .Padding(1, 0)
            .Expand();
    }

    private static Panel BuildKeys() =>
        new Panel(new Markup(
                $"[#BB86FC on #6C3483] P [/][white on #6C3483] pause [/]  "
                + $"[#BB86FC on #6C3483] R [/][white on #6C3483] resume [/]  "
                + $"[#BB86FC on #6C3483] Q [/][white on #6C3483]/[/][#BB86FC on #6C3483] Esc [/][white on #6C3483] stop [/]"))
            .Header($"[{Theme.Magenta}]KEYS[/]")
            .BorderColor(Purple)
            .Border(BoxBorder.Rounded)
            .Padding(1, 0)
            .Expand();

    private static Panel PurplePanel(IRenderable body, string header) =>
        new Panel(body)
            .Header(header)
            .BorderColor(Purple)
            .Border(BoxBorder.Rounded)
            .Padding(1, 0)
            .Expand();



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


}
