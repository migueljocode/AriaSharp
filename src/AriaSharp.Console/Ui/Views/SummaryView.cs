


namespace AriaSharp.App;

/// <summary>
/// Post-run summary: purple Spectre panels, or plain text for --plain.
/// </summary>
internal static class SummaryView
{
    private static readonly Color Purple = new(155, 89, 182);
    private static readonly Color PurpleDeep = new(108, 52, 131);

    public static string Render(
        SessionState state,
        RunOutcome outcome,
        TimeSpan elapsed,
        string outputDir,
        string? sessionFile) =>
        BuildPlain(state, outcome, elapsed, outputDir, sessionFile);

    public static IRenderable Build(
        SessionState state,
        RunOutcome outcome,
        TimeSpan elapsed,
        string outputDir,
        string? sessionFile,
        bool showRetryPrompt = false)
    {
        var rows = new List<IRenderable>();

        var (title, style) = outcome.Kind switch
        {
            RunOutcomeKind.Completed => ("COMPLETED", "green"),
            RunOutcomeKind.Failed => ("FAILED", "red"),
            RunOutcomeKind.Interrupted => ("INTERRUPTED", "yellow"),
            RunOutcomeKind.Empty => ("EMPTY", "grey"),
            _ => ("DONE", Theme.PurpleBright),
        };

        var avgSpeed = elapsed.TotalSeconds > 0
            ? (long)(state.CompletedLength / elapsed.TotalSeconds)
            : 0L;

        var grid = new Grid().AddColumn(new GridColumn().NoWrap()).AddColumn();
        grid.AddRow(
            new Markup($"[{Theme.Muted}]Result[/]"),
            new Markup($"[{style}]{title}[/]"
                + (outcome.Kind == RunOutcomeKind.Failed && !string.IsNullOrEmpty(outcome.Message)
                    ? $"  [grey]{Markup.Escape(TrimMsg(outcome.Message))}[/]"
                    : "")));
        grid.AddRow(
            new Markup($"[{Theme.Muted}]Size[/]"),
            new Markup($"[white]{FormatHelpers.FormatBytes(state.CompletedLength)}[/]"
                + (state.TotalLength > 0
                    ? $" [dim]/ {FormatHelpers.FormatBytes(state.TotalLength)}[/]"
                    : "")));
        grid.AddRow(
            new Markup($"[{Theme.Muted}]Speed[/]"),
            new Markup($"[white]{FormatHelpers.FormatSpeed(avgSpeed)}[/] avg"
                + (state.TotalDownloadSpeed > 0
                    ? $", last {FormatHelpers.FormatSpeed(state.TotalDownloadSpeed)}"
                    : "")));
        grid.AddRow(
            new Markup($"[{Theme.Muted}]Time[/]"),
            new Markup($"[white]{FormatElapsed(elapsed)}[/]"));
        grid.AddRow(
            new Markup($"[{Theme.Muted}]Output[/]"),
            new Markup($"[{Theme.Accent}]{Markup.Escape(outputDir)}[/]"));
        if (!string.IsNullOrEmpty(sessionFile) && File.Exists(sessionFile))
        {
            grid.AddRow(
                new Markup($"[{Theme.Muted}]Resume[/]"),
                new Markup($"[dim]{Markup.Escape(sessionFile)}[/]"));
        }

        rows.Add(new Panel(grid)
            .Header($"[{Theme.Magenta}]SUMMARY[/]")
            .BorderColor(Purple)
            .Padding(1, 0)
            .Expand());

        if (state.CompleteItems.Count > 0)
        {
            var lines = state.CompleteItems.Take(8).Select(i =>
                new Markup(
                    $"[green]✓[/] [white]{Markup.Escape(FormatHelpers.Truncate(
                        string.IsNullOrEmpty(i.Name) ? i.Gid : i.Name, 40))}[/] "
                    + $"[dim]{FormatHelpers.FormatBytes(i.CompletedLength)}[/]"));
            rows.Add(new Panel(new Rows(lines))
                .Header($"[green]COMPLETE ({state.CompleteItems.Count})[/]")
                .BorderColor(Color.Green)
                .Padding(1, 0)
                .Expand());
        }

        if (state.FailedItems.Count > 0)
        {
            var lines = state.FailedItems.Take(8).Select(i =>
            {
                var name = string.IsNullOrEmpty(i.Name) ? i.Gid : i.Name;
                var err = i.ErrorMessage
                    ?? (i.ErrorCode != 0 ? ErrorCatalog.TextFor(i.ErrorCode) : "error");
                return new Markup(
                    $"[red]✗[/] [white]{Markup.Escape(FormatHelpers.Truncate(name, 28))}[/] "
                    + $"[grey]{Markup.Escape(FormatHelpers.Truncate(err, 48))}[/]");
            });
            rows.Add(new Panel(new Rows(lines))
                .Header($"[red]FAILED ({state.FailedItems.Count})[/]")
                .BorderColor(Color.Red)
                .Padding(1, 0)
                .Expand());
        }

        if (showRetryPrompt)
        {
            rows.Add(new Panel(new Markup(
                    $"[{Theme.PurpleBright}]R[/] [{Theme.Muted}]retry[/]    "
                    + $"[{Theme.PurpleBright}]Q[/] [{Theme.Muted}]quit[/]    "
                    + $"[dim](single key — no Enter)[/]"))
                .Header($"[{Theme.Magenta}]NEXT[/]")
                .BorderColor(PurpleDeep)
                .Padding(1, 0)
                .Expand());
        }

        return new Rows(rows);
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
        if (color && !System.Console.IsOutputRedirected)
        {
            try
            {
                AnsiConsole.Write(Build(state, outcome, elapsed, outputDir, sessionFile, showRetryPrompt));
                return;
            }
            catch (InvalidOperationException) { }
            catch (IOException) { }
        }

        System.Console.Out.Write(BuildPlain(state, outcome, elapsed, outputDir, sessionFile));
        if (showRetryPrompt)
            System.Console.Out.WriteLine("  [R]etry / [Q]uit  (single key)");
    }

    private static string BuildPlain(
        SessionState state,
        RunOutcome outcome,
        TimeSpan elapsed,
        string outputDir,
        string? sessionFile)
    {
        var sb = new StringBuilder(512);
        var result = outcome.Kind switch
        {
            RunOutcomeKind.Completed => "ok",
            RunOutcomeKind.Failed => "failed",
            RunOutcomeKind.Interrupted => "interrupted",
            RunOutcomeKind.Empty => "empty",
            _ => "unknown",
        };
        if (outcome.Kind == RunOutcomeKind.Failed && !string.IsNullOrEmpty(outcome.Message))
            result = $"failed — {outcome.Message}";

        var avgSpeed = elapsed.TotalSeconds > 0
            ? (long)(state.CompletedLength / elapsed.TotalSeconds)
            : 0L;

        sb.AppendLine("Summary");
        sb.AppendLine($"  Result   {result}");
        sb.AppendLine($"  Size     {FormatHelpers.FormatBytes(state.CompletedLength)}"
            + (state.TotalLength > 0 ? $" / {FormatHelpers.FormatBytes(state.TotalLength)}" : ""));
        sb.AppendLine($"  Speed    {FormatHelpers.FormatSpeed(avgSpeed)} avg"
            + (state.TotalDownloadSpeed > 0 ? $", {FormatHelpers.FormatSpeed(state.TotalDownloadSpeed)} last" : ""));
        sb.AppendLine($"  Time     {FormatElapsed(elapsed)}");
        sb.AppendLine($"  Output   {outputDir}");
        if (!string.IsNullOrEmpty(sessionFile) && File.Exists(sessionFile))
            sb.AppendLine($"  Resume   {sessionFile}");
        foreach (var item in state.CompleteItems.Take(8))
            sb.AppendLine($"  + {FormatHelpers.Truncate(string.IsNullOrEmpty(item.Name) ? item.Gid : item.Name, 40)}");
        foreach (var item in state.FailedItems.Take(8))
        {
            var err = item.ErrorMessage ?? ErrorCatalog.TextFor(item.ErrorCode);
            sb.AppendLine($"  x {FormatHelpers.Truncate(string.IsNullOrEmpty(item.Name) ? item.Gid : item.Name, 28)} — {err}");
        }
        return sb.ToString();
    }

    private static string TrimMsg(string msg) =>
        msg.Length <= 60 ? msg : msg[..57] + "...";

    private static string FormatElapsed(TimeSpan ts) =>
        ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}"
            : $"{(int)ts.TotalMinutes}:{ts.Seconds:D2}";
}
