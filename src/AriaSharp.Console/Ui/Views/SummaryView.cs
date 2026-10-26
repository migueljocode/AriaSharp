using System.Text;

using AriaSharp.Models;

namespace AriaSharp.App;

/// <summary>
/// Post-run summary card (dneat parity rows: Result, Size, Speed, Time, Output, Resume).
/// </summary>
internal static class SummaryView
{
    public static string Render(SessionState state, RunOutcome outcome, TimeSpan elapsed, string outputDir, string? sessionFile)
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
        sb.AppendLine($"  Time     {FormatDuration(elapsed)}");
        sb.AppendLine($"  Output   {outputDir}");
        if (!string.IsNullOrEmpty(sessionFile) && File.Exists(sessionFile))
        {
            var len = new FileInfo(sessionFile).Length;
            sb.AppendLine(len > 0
                ? $"  Resume   {sessionFile} ({len} B)"
                : $"  Resume   (session empty — deleted if configured)");
        }

        if (state.FailedItems.Count > 0)
        {
            sb.AppendLine("  Failed");
            foreach (var item in state.FailedItems.Take(5))
            {
                var name = string.IsNullOrEmpty(item.Name) ? item.Gid : item.Name;
                var err = item.ErrorMessage
                    ?? (item.ErrorCode != 0 ? ErrorCatalog.TextFor(item.ErrorCode) : "error");
                sb.AppendLine($"    - {name}: {err}");
            }
            if (state.FailedItems.Count > 5)
                sb.AppendLine($"    … and {state.FailedItems.Count - 5} more");
        }

        if (state.CompleteItems.Count > 0 && state.CompleteItems.Count <= 8)
        {
            sb.AppendLine("  Complete");
            foreach (var item in state.CompleteItems)
            {
                var name = string.IsNullOrEmpty(item.Name) ? item.Gid : item.Name;
                sb.AppendLine($"    - {name} ({FormatHelpers.FormatBytes(item.CompletedLength)})");
            }
        }
        else if (state.CompleteItems.Count > 8)
        {
            sb.AppendLine($"  Complete {state.CompleteItems.Count} files");
        }

        return sb.ToString();
    }

    private static string FormatDuration(TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}h {ts.Minutes:D2}m {ts.Seconds:D2}s";
        if (ts.TotalMinutes >= 1)
            return $"{(int)ts.TotalMinutes}m {ts.Seconds:D2}s";
        return $"{ts.TotalSeconds:F1}s";
    }
}
