namespace AriaSharp.Services;

/// <summary>
/// Resolves download inputs in Brief order: CLI args → stdin (if piped) → clipboard (interactive).
/// </summary>
public static class InputResolver
{
    public static async Task<IReadOnlyList<string>> ResolveAsync(
        IReadOnlyList<string> existing,
        IUrlExtractor extractor,
        IClipboardReader clipboard,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(extractor);
        ArgumentNullException.ThrowIfNull(clipboard);

        if (existing.Count > 0)
            return existing.ToArray();

        // 1. stdin when piped
        if (Console.IsInputRedirected)
        {
            using var reader = new StreamReader(Console.OpenStandardInput());
            var text = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
            var fromStdin = extractor.Extract(text);
            if (fromStdin.Length > 0)
                return fromStdin;
            // whole stdin may be a single path/URL per line without needing regex
            var lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (lines.Length > 0)
                return lines;
        }

        // 2. clipboard when interactive
        if (!Console.IsInputRedirected && !Console.IsOutputRedirected)
        {
            try
            {
                var clip = await clipboard.ReadTextAsync(ct).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(clip))
                {
                    var fromClip = extractor.Extract(clip);
                    if (fromClip.Length > 0)
                        return fromClip;
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                // clipboard unavailable — fall through
            }
        }

        return Array.Empty<string>();
    }
}
