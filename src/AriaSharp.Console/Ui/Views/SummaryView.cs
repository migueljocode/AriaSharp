namespace AriaSharp.App;

/// <summary>
/// End-of-run summary: Spectre panels or plain text. Shared label helpers (DRY).
/// </summary>
internal static class SummaryView
{
    private static readonly Color Purple = new(155, 89, 182);

    public static string Render(
        SessionState state,
        RunOutcome outcome,
        TimeSpan elapsed,
        string outputDir,
        string? sessionFile = null) =>
        BuildPlain(state, outcome, elapsed, outputDir, sessionFile);

    public static IRenderable Build(
        SessionState state,
        RunOutcome outcome,
        TimeSpan elapsed,
        string outputDir,
        string? sessionFile = null,
        bool showRetryPrompt = false)
    {
        ThrowIfNull(state);
        ThrowIfNull(outcome);

        var root = new Grid().Expand();
        root.AddColumn(new GridColumn().NoWrap());

        var title = outcome.Kind switch
        {
            RunOutcomeKind.Completed => "[green]COMPLETE[/]",
            RunOutcomeKind.Failed => "[red]FAILED[/]",
            RunOutcomeKind.Interrupted => "[yellow]INTERRUPTED[/]",
            RunOutcomeKind.Empty => "[grey]EMPTY[/]",
            _ => "[grey]DONE[/]",
        };

        var avgSpeed = AverageSpeed(state, elapsed);
        var rows = new List<IRenderable>
        {
            new Markup($"[white]{Markup.Escape(ResultLabel(outcome))}[/]"),
            new Markup($"[grey]{FormatBytes(state.CompletedLength)}[/]"
                + (state.TotalLength > 0 ? $" [dim]/ {FormatBytes(state.TotalLength)}[/]" : "")),
            new Markup($"[white]{FormatSpeed(avgSpeed)}[/] avg"
                + (state.TotalDownloadSpeed > 0 ? $", last {FormatSpeed(state.TotalDownloadSpeed)}" : "")),
            new Markup($"[dim]{elapsed.AsElapsed()}[/]"),
            new Markup($"[dim]{Markup.Escape(outputDir)}[/]"),
        };
        if (!string.IsNullOrEmpty(sessionFile) && File.Exists(sessionFile))
            rows.Add(new Markup($"[dim]resume {Markup.Escape(sessionFile)}[/]"));

        root.AddRow(new Panel(new Rows(rows))
            .Header(title)
            .BorderColor(Purple)
            .Padding(1, 0)
            .Expand());

        if (state.CompleteItems is { Count: > 0 } complete)
        {
            root.AddRow(ItemPanel(
                $"[green]COMPLETE ({complete.Count})[/]",
                Color.Green,
                complete.Take(8).Select(static i =>
                    $"[green]✓[/] [white]{Markup.Escape(ItemName(i).TruncateTo(40))}[/]  [dim]{FormatBytes(i.CompletedLength)}[/]")));
        }

        if (state.FailedItems is { Count: > 0 } failed)
        {
            root.AddRow(ItemPanel(
                $"[red]FAILED ({failed.Count})[/]",
                Color.Red,
                failed.Take(8).Select(static i =>
                    $"[red]✗[/] [white]{Markup.Escape(ItemName(i).TruncateTo(28))}[/]  [grey]{Markup.Escape(ItemError(i))}[/]")));
        }

        if (showRetryPrompt)
        {
            root.AddRow(new Markup(
                $"[{Theme.PurpleBright}]R[/] [{Theme.Muted}]retry[/]   "
                + $"[{Theme.PurpleBright}]Q[/] [{Theme.Muted}]quit[/]"));
        }

        return root;
    }

    public static void Write(
        SessionState state,
        RunOutcome outcome,
        TimeSpan elapsed,
        string outputDir,
        string? sessionFile,
        bool color,
        bool showRetryPrompt = false)
    {
        ThrowIfNull(state);
        ThrowIfNull(outcome);

        if (color && !System.Console.IsOutputRedirected)
        {
            try
            {
                AnsiConsole.Write(Build(state, outcome, elapsed, outputDir, sessionFile, showRetryPrompt));
                return;
            }
            catch (Exception ex) when (ex is InvalidOperationException or IOException) { }
        }

        System.Console.Out.Write(BuildPlain(state, outcome, elapsed, outputDir, sessionFile));
        if (showRetryPrompt)
            System.Console.Out.WriteLine("  [R]etry / [Q]uit  (single key)");
    }

    private static Panel ItemPanel(string header, Color border, IEnumerable<string> lines) =>
        new Panel(new Rows(lines.Select(static l => new Markup(l))))
            .Header(header)
            .BorderColor(border)
            .Padding(1, 0)
            .Expand();

    private static string BuildPlain(
        SessionState state,
        RunOutcome outcome,
        TimeSpan elapsed,
        string outputDir,
        string? sessionFile)
    {
        var sb = new StringBuilder(512);
        var avgSpeed = AverageSpeed(state, elapsed);

        sb.AppendLine("Summary");
        sb.AppendLine($"  Result   {ResultLabel(outcome)}");
        sb.AppendLine($"  Size     {FormatBytes(state.CompletedLength)}"
            + (state.TotalLength > 0 ? $" / {FormatBytes(state.TotalLength)}" : ""));
        sb.AppendLine($"  Speed    {FormatSpeed(avgSpeed)} avg"
            + (state.TotalDownloadSpeed > 0 ? $", {FormatSpeed(state.TotalDownloadSpeed)} last" : ""));
        sb.AppendLine($"  Time     {elapsed.AsElapsed()}");
        sb.AppendLine($"  Output   {outputDir}");
        if (!string.IsNullOrEmpty(sessionFile) && File.Exists(sessionFile))
            sb.AppendLine($"  Resume   {sessionFile}");
        foreach (var item in state.CompleteItems.Take(8))
            sb.AppendLine($"  + {ItemName(item).TruncateTo(40)}");
        foreach (var item in state.FailedItems.Take(8))
            sb.AppendLine($"  x {ItemName(item).TruncateTo(28)} — {ItemError(item)}");
        return sb.ToString();
    }

    private static string ResultLabel(RunOutcome outcome) => outcome.Kind switch
    {
        RunOutcomeKind.Completed => "ok",
        RunOutcomeKind.Failed when outcome.Message is { Length: > 0 } m => $"failed — {m}",
        RunOutcomeKind.Failed => "failed",
        RunOutcomeKind.Interrupted => "interrupted",
        RunOutcomeKind.Empty => "empty",
        _ => "unknown",
    };

    private static string ItemName(DownloadItem item) =>
        string.IsNullOrEmpty(item.Name) ? item.Gid : item.Name;

    private static string ItemError(DownloadItem item) =>
        item.ErrorMessage ?? TextFor(item.ErrorCode);

    private static long AverageSpeed(SessionState state, TimeSpan elapsed) =>
        elapsed.TotalSeconds > 0 ? (long)(state.CompletedLength / elapsed.TotalSeconds) : 0L;

}
