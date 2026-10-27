namespace AriaSharp.Services;

/// <summary>
/// Smart input resolution as C# 14 extension members on argument lists.
/// Console I/O is injected by the caller (stdin / clipboard policy).
/// </summary>
public static class InputResolver
{
    extension(IReadOnlyList<string> existing)
    {
        public async Task<IReadOnlyList<string>> ResolveDownloadUrlsAsync(
            IUrlExtractor extractor,
            IUrlClassifier classifier,
            IClipboardReader clipboard,
            bool forceClipboard,
            IReadOnlyList<string>? forceFiles,
            CancellationToken ct,
            TextReader? stdin = null,
            bool allowClipboardFallback = false)
        {
            ThrowIfNull(extractor);
            ThrowIfNull(classifier);
            ThrowIfNull(clipboard);

            forceFiles ??= [];

            if (forceClipboard)
                return await FromClipboardAsync(extractor, clipboard, ct);

            if (forceFiles is { Count: > 0 })
                return await FromFilesAsync(forceFiles, extractor, ct);

            if (existing is { Count: > 0 })
                return await ExpandArgsAsync(existing, extractor, classifier, ct);

            if (stdin is not null)
            {
                var text = await stdin.ReadToEndAsync(ct);
                var fromStdin = extractor.Extract(text);
                if (fromStdin is { Length: > 0 })
                    return fromStdin;

                string[] lines = [.. text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
                if (lines is { Length: > 0 })
                    return await ExpandArgsAsync(lines, extractor, classifier, ct);
            }

            if (allowClipboardFallback)
                return await FromClipboardAsync(extractor, clipboard, ct);

            return [];
        }
    }

    public static Task<IReadOnlyList<string>> ResolveAsync(
        IReadOnlyList<string> existing,
        IUrlExtractor extractor,
        IUrlClassifier classifier,
        IClipboardReader clipboard,
        bool forceClipboard,
        IReadOnlyList<string>? forceFiles,
        CancellationToken ct,
        TextReader? stdin = null,
        bool allowClipboardFallback = false)
    {
        ThrowIfNull(existing);
        return existing.ResolveDownloadUrlsAsync(
            extractor, classifier, clipboard, forceClipboard, forceFiles, ct, stdin, allowClipboardFallback);
    }

    private static async Task<IReadOnlyList<string>> ExpandArgsAsync(
        IReadOnlyList<string> args,
        IUrlExtractor extractor,
        IUrlClassifier classifier,
        CancellationToken ct)
    {
        List<string> result = [];
        HashSet<string> seen = new(StringComparer.Ordinal);

        foreach (var arg in args)
        {
            if (string.IsNullOrWhiteSpace(arg))
                continue;

            if (classifier.IsUrl(arg))
            {
                if (seen.Add(arg))
                    result.Add(arg);
                continue;
            }

            if (File.Exists(arg))
            {
                var fromFile = await ReadFileUrlsAsync(arg, extractor, ct);
                foreach (var u in fromFile)
                {
                    if (seen.Add(u))
                        result.Add(u);
                }
                if (fromFile is { Length: 0 } && classifier.IsSessionFile(arg) && seen.Add(arg))
                    result.Add(arg);
                continue;
            }

            var extracted = extractor.Extract(arg);
            if (extracted is { Length: > 0 })
            {
                foreach (var u in extracted)
                {
                    if (seen.Add(u))
                        result.Add(u);
                }
                continue;
            }

            if (seen.Add(arg))
                result.Add(arg);
        }

        return result;
    }

    private static async Task<IReadOnlyList<string>> FromFilesAsync(
        IReadOnlyList<string> files,
        IUrlExtractor extractor,
        CancellationToken ct)
    {
        List<string> result = [];
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (var path in files)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"List file not found: {path}", path);
            foreach (var u in await ReadFileUrlsAsync(path, extractor, ct))
            {
                if (seen.Add(u))
                    result.Add(u);
            }
        }
        return result;
    }

    private static async Task<string[]> ReadFileUrlsAsync(string path, IUrlExtractor extractor, CancellationToken ct)
    {
        var text = await File.ReadAllTextAsync(path, ct);
        var urls = extractor.Extract(text);
        if (urls is { Length: > 0 })
            return urls;

        return [.. text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static l => !l.StartsWith('#') && !string.IsNullOrWhiteSpace(l))];
    }

    private static async Task<IReadOnlyList<string>> FromClipboardAsync(
        IUrlExtractor extractor,
        IClipboardReader clipboard,
        CancellationToken ct)
    {
        try
        {
            var clip = await clipboard.ReadTextAsync(ct);
            if (string.IsNullOrWhiteSpace(clip))
                return [];
            var urls = extractor.Extract(clip);
            if (urls is { Length: > 0 })
                return urls;
            return [.. clip.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return [];
        }
    }
}
