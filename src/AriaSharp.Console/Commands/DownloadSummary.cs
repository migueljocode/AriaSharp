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

        if (outcome.Kind != RunOutcomeKind.Empty)
        {
            SummaryView.Write(state, outcome, elapsed, outputDir, sessionPath, color, showRetryPrompt: canPrompt);
        }

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

        if (outcome.Kind is RunOutcomeKind.Empty
            && Directory.Exists(outputDir)
            && DirectoryIsEmpty(outputDir))
        {
            try { Directory.Delete(outputDir); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        return (outcome.ToExitCode(), retry);
    }

    private static bool DirectoryIsEmpty(string path)
    {
        try
        {
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }
}
