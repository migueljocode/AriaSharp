using System.IO;
using System.Runtime.CompilerServices;

namespace AriaSharp.Services;

public static class SessionFile
{
    private static readonly string[] UrlSchemes =
    {
        "http://", "https://", "ftp://", "ftps://", "sftp://", "magnet:?"
    };

    private static readonly string[] Aria2OptionKeys =
    {
        "out=", "dir=", "continue=", "split=", "max-connection-per-server=",
        "min-split-size=", "save-session=", "rpc-secret=", "max-overall-download-limit="
    };

    public static IAsyncEnumerable<string> EnumerateEntriesAsync(string path, CancellationToken ct = default)
    {
        if (!File.Exists(path)) return AsyncEnumerable.Empty<string>();
        return EnumerateEntriesCore(path, ct);

        static async IAsyncEnumerable<string> EnumerateEntriesCore(string path, [EnumeratorCancellation] CancellationToken ct)
        {
            // aria2 may be rewriting the session file while we read it
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
            using var reader = new StreamReader(stream);
            while (await reader.ReadLineAsync(ct).ConfigureAwait(false) is { } line)
            {
                ct.ThrowIfCancellationRequested();
                var span = line.AsSpan().Trim();
                if (span.IsEmpty) continue;
                if (IsUrlLine(span)) yield return line;
            }
        }
    }

    public static bool HasEntries(string path)
    {
        if (!File.Exists(path)) return false;
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);
        string? line;
        while ((line = reader.ReadLine()) is { } l)
        {
            var span = l.AsSpan().Trim();
            if (span.IsEmpty) continue;
            if (IsUrlLine(span)) return true;
        }
        return false;
    }

    public static bool IsAria2ListFile(string value)
    {
        if (string.IsNullOrEmpty(value) || !File.Exists(value)) return false;
        try
        {
            var urlSeen = false;
            var optionSeen = false;
            using var stream = new FileStream(value, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
            using var reader = new StreamReader(stream);
            string? line;
            while ((line = reader.ReadLine()) is { } l)
            {
                var span = l.AsSpan().Trim();
                if (span.IsEmpty) continue;
                if (IsUrlLine(span)) { urlSeen = true; continue; }
                if (IsRecognisedOption(span)) optionSeen = true;
            }
            return urlSeen || optionSeen;
        }
        catch (IOException) { return false; }
    }

    public static void Delete(string path)
    {
        try { File.Delete(path); }
        catch (FileNotFoundException) { }
        catch (DirectoryNotFoundException) { }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    public static Task DeleteAsync(string path, CancellationToken ct = default)
    {
        Delete(path);
        return Task.CompletedTask;
    }

    private static bool IsUrlLine(ReadOnlySpan<char> line)
    {
        foreach (var scheme in UrlSchemes)
            if (line.StartsWith(scheme, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private static bool IsRecognisedOption(ReadOnlySpan<char> line)
    {
        foreach (var key in Aria2OptionKeys)
            if (line.StartsWith(key, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
}
