using System.Runtime.CompilerServices;

namespace AriaSharp.Services;

/// <summary>Session/list file helpers as C# 14 extension members on path strings.</summary>
public static class SessionFile
{
    private static readonly string[] UrlSchemes =
    [
        "http://", "https://", "ftp://", "ftps://", "sftp://", "magnet:?",
    ];

    private static readonly string[] Aria2OptionKeys =
    [
        "out=", "dir=", "continue=", "split=", "max-connection-per-server=",
        "min-split-size=", "save-session=", "rpc-secret=", "max-overall-download-limit=",
    ];

    extension(string path)
    {
        public async IAsyncEnumerable<string> EnumerateSessionEntriesAsync(
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            if (!File.Exists(path))
                yield break;

            using var stream = new FileStream(
                path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete,
                4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
            using var reader = new StreamReader(stream);
            while (await reader.ReadLineAsync(ct) is { } line)
            {
                ct.ThrowIfCancellationRequested();
                var span = line.AsSpan().Trim();
                if (span.IsEmpty) continue;
                if (IsUrlLine(span)) yield return line;
            }
        }

        public bool SessionHasEntries()
        {
            if (!File.Exists(path)) return false;
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream);
            while (reader.ReadLine() is { } line)
            {
                var span = line.AsSpan().Trim();
                if (span.IsEmpty) continue;
                if (IsUrlLine(span)) return true;
            }
            return false;
        }

        public bool LooksLikeAria2ListFile()
        {
            if (path is not { Length: > 0 } || !File.Exists(path)) return false;
            try
            {
                var urlSeen = false;
                var optionSeen = false;
                using var stream = new FileStream(
                    path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete,
                    4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
                using var reader = new StreamReader(stream);
                while (reader.ReadLine() is { } line)
                {
                    var span = line.AsSpan().Trim();
                    if (span.IsEmpty) continue;
                    if (IsUrlLine(span)) { urlSeen = true; continue; }
                    if (IsRecognisedOption(span)) optionSeen = true;
                }
                return urlSeen || optionSeen;
            }
            catch (IOException) { return false; }
        }

        public void DeleteSessionFile()
        {
            try { File.Delete(path); }
            catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException or IOException or UnauthorizedAccessException) { }
        }

        public Task DeleteSessionFileAsync(CancellationToken ct = default)
        {
            _ = ct;
            path.DeleteSessionFile();
            return Task.CompletedTask;
        }
    }

    // Compatibility wrappers for existing call sites / global static style
    public static IAsyncEnumerable<string> EnumerateEntriesAsync(string path, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(path);
        return path.EnumerateSessionEntriesAsync(ct);
    }

    public static bool HasEntries(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return path.SessionHasEntries();
    }

    public static bool IsAria2ListFile(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.LooksLikeAria2ListFile();
    }

    public static void Delete(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        path.DeleteSessionFile();
    }

    public static Task DeleteAsync(string path, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(path);
        return path.DeleteSessionFileAsync(ct);
    }

    private static bool IsUrlLine(ReadOnlySpan<char> span)
    {
        foreach (var scheme in UrlSchemes)
        {
            if (span.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static bool IsRecognisedOption(ReadOnlySpan<char> span)
    {
        if (span.Length > 0 && span[0] == ';')
            span = span[1..].TrimStart();
        foreach (var key in Aria2OptionKeys)
        {
            if (span.StartsWith(key, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
