namespace AriaSharp.App;

/// <summary>
/// Resolves CLI / files / stdin / clipboard into concrete URL inputs.
/// </summary>
internal static class InputCommand
{
    public static async Task<(int? ExitCode, AppSettings Settings)> ResolveAsync(AppSettings settings)
    {
        if (await ApplySessionAsync(settings, Directory.GetCurrentDirectory()) is int sessionExit)
            return (sessionExit, settings);

        try
        {
            TextReader? stdin = Console.IsInputRedirected
                ? new StreamReader(Console.OpenStandardInput())
                : null;
            try
            {
                var (resolved, rejected) = await settings.Inputs.ResolveDownloadUrlsAsync(
                    new UrlExtractor(),
                    new UrlClassifier(),
                    new ClipboardReader(),
                    forceClipboard: settings.FromClipboard,
                    forceFiles: settings.FromFiles,
                    CancellationToken.None,
                    stdin: stdin,
                    allowClipboardFallback: !Console.IsOutputRedirected);

                settings.Inputs = [.. resolved];

                if (resolved is { Count: 0 } && rejected is { Count: > 0 })
                {
                    await Console.Error.WriteLineAsync($"Error: no valid URL, magnet or file in: {string.Join(' ', rejected)}");
                    return ((int)ExitCode.UsageError, settings);
                }

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

        if (settings.Inputs is { Count: 0 } && !settings.Resume)
        {
            await Console.Out.WriteLineAsync(
                "No URLs found (CLI, files, stdin, or clipboard). Use --help, --from-clipboard, or --from-file.");
            return ((int)ExitCode.UsageError, settings);
        }

        return (null, settings);
    }

    /// <summary>
    /// -s / --session implies resume. "" (bare -s) means auto: resume.session in <paramref name="cwd"/>.
    /// Stores the resolved session file and, unless -o was given, the resolved output directory.
    /// Returns a usage exit code (after printing the error) when the session cannot be found.
    /// </summary>
    internal static async Task<int?> ApplySessionAsync(AppSettings settings, string cwd)
    {
        if (settings.SessionFile is null)
            return null;

        try
        {
            var located = SessionLocator.Locate(settings.SessionFile, cwd, settings.OutputDir);
            settings.SessionFile = located.SessionFile;
            if (string.IsNullOrWhiteSpace(settings.OutputDir))
                settings.OutputDir = located.OutputDir;
            settings.Resume = true;
            return null;
        }
        catch (SessionNotFoundException ex)
        {
            await Console.Error.WriteLineAsync($"Error: {ex.Message}");
            return (int)ExitCode.UsageError;
        }
    }
}
