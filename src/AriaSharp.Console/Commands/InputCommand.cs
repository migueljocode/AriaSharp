namespace AriaSharp.App;

/// <summary>
/// Resolves CLI / files / stdin / clipboard into concrete URL inputs.
/// </summary>
internal static class InputCommand
{
    public static async Task<(int? ExitCode, AppSettings Settings)> ResolveAsync(AppSettings settings)
    {
        try
        {
            var resolved = await InputResolver.ResolveAsync(
                    settings.Inputs,
                    new UrlExtractor(),
                    new UrlClassifier(),
                    new ClipboardReader(),
                    forceClipboard: settings.FromClipboard,
                    forceFiles: settings.FromFiles,
                    CancellationToken.None)
                .ConfigureAwait(false);

            settings.Inputs = [.. resolved];
            if (settings.Verbose && resolved is { Count: > 0 })
            {
                await Console.Error.WriteLineAsync($"Resolved {resolved.Count} download URL(s).")
                    .ConfigureAwait(false);
            }
        }
        catch (FileNotFoundException ex)
        {
            await Console.Error.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
            return ((int)ExitCode.UsageError, settings);
        }

        if (settings.Inputs is { Count: 0 })
        {
            await Console.Out.WriteLineAsync(
                    "No URLs found (CLI, files, stdin, or clipboard). Use --help, --from-clipboard, or --from-file.")
                .ConfigureAwait(false);
            return ((int)ExitCode.UsageError, settings);
        }

        return (null, settings);
    }
}
