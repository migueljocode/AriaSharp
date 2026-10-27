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
            TextReader? stdin = Console.IsInputRedirected
                ? new StreamReader(Console.OpenStandardInput())
                : null;
            try
            {
                var resolved = await settings.Inputs.ResolveDownloadUrlsAsync(
                    new UrlExtractor(),
                    new UrlClassifier(),
                    new ClipboardReader(),
                    forceClipboard: settings.FromClipboard,
                    forceFiles: settings.FromFiles,
                    CancellationToken.None,
                    stdin: stdin,
                    allowClipboardFallback: !Console.IsOutputRedirected);

                settings.Inputs = [.. resolved];
                if (settings.Verbose && resolved is { Count: > 0 })
                    await Console.Error.WriteLineAsync($"Resolved {resolved.Count} download URL(s).");
            }
            finally
            {
                stdin?.Dispose();
            }
        }
        catch (FileNotFoundException ex)
        {
            await Console.Error.WriteLineAsync($"Error: {ex.Message}");
            return ((int)ExitCode.UsageError, settings);
        }

        if (settings.Inputs is { Count: 0 })
        {
            await Console.Out.WriteLineAsync(
                "No URLs found (CLI, files, stdin, or clipboard). Use --help, --from-clipboard, or --from-file.");
            return ((int)ExitCode.UsageError, settings);
        }

        return (null, settings);
    }
}
