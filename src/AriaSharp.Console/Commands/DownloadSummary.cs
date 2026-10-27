namespace AriaSharp.App;

/// <summary>End-of-run summary, retry prompt, empty session cleanup.</summary>
internal static class DownloadSummary
{
    public static async Task<(int Code, bool Retry)> FinishAsync(
        SessionState state,
        RunOutcome outcome,
        TimeSpan elapsed,
        string outputDir,
        string sessionPath,
        AppSettings settings)
    {
        var color = Theme.Enabled && !settings.NoColor && !settings.Plain
            && !Console.IsOutputRedirected;
        var canInteract = !settings.Yes
            && !settings.Plain
            && !Console.IsInputRedirected
            && !Console.IsOutputRedirected;
        var canPrompt = outcome.Kind == RunOutcomeKind.Failed && canInteract;

        SummaryView.Write(state, outcome, elapsed, outputDir, sessionPath, color, showRetryPrompt: canPrompt);

        var retry = false;
        if (canPrompt)
            retry = KeyListener.WaitRetryOrQuit();

        if (outcome.Kind is RunOutcomeKind.Failed or RunOutcomeKind.Empty
            && outcome.Message is { Length: > 0 } msg)
            await Console.Error.WriteLineAsync($"Error: {msg}");

        try
        {
            if (sessionPath is { Length: > 0 } && File.Exists(sessionPath) && !SessionFile.HasEntries(sessionPath))
                await SessionFile.DeleteAsync(sessionPath);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }

        return (outcome.ToExitCode(), retry);
    }
}
