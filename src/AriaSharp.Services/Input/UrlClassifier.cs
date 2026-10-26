using System.Buffers;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using AriaSharp.Models;

namespace AriaSharp.Services;

public interface IUrlExtractor { string[] Extract(string text); }
public interface IUrlClassifier
{
    bool IsUrl(string value);
    bool IsListFile(string value);
    bool IsSessionFile(string value);
    bool IsAria2ListFile(string value);
}
public interface IUrlSanitizer { string Sanitize(string value); string MaskCredentials(string value); }

public sealed partial class UrlExtractor : IUrlExtractor
{
    // magnet:? has no "//"; other schemes use ://
    [GeneratedRegex(@"(?:(?<scheme>https?|ftp|ftps|sftp):\/\/|(?<scheme>magnet):\?)[^\s""'<>]+", RegexOptions.IgnoreCase)]
    private static partial Regex UrlPattern();

    private static readonly SearchValues<char> TrailingPunctuation = SearchValues.Create(",;)]}'.!?");

    public string[] Extract(string text)
    {
        if (string.IsNullOrEmpty(text)) return [];
        var list = new List<string>();
        foreach (Match m in UrlPattern().Matches(text))
        {
            var span = m.Value.AsSpan();
            while (!span.IsEmpty && TrailingPunctuation.Contains(span[^1])) span = span[..^1];
            var url = span.ToString();
            if (!string.IsNullOrEmpty(url)) list.Add(url);
        }
        return [.. list];
    }
}

public sealed class UrlClassifier : IUrlClassifier
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

    public bool IsUrl(string value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        var span = value.AsSpan();
        foreach (var scheme in UrlSchemes)
            if (span.StartsWith(scheme, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    public bool IsListFile(string value) => File.Exists(value);

    public bool IsSessionFile(string value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        var name = Path.GetFileName(value);
        return name.Equals("resume.session", StringComparison.OrdinalIgnoreCase) || value.EndsWith(".session", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsAria2ListFile(string value)
    {
        if (string.IsNullOrEmpty(value) || !File.Exists(value)) return false;
        try
        {
            var urlSeen = false;
            using var stream = new FileStream(value, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
            using var reader = new StreamReader(stream);
            string? line;
            while ((line = reader.ReadLine()) is { } l)
            {
                var span = l.AsSpan().Trim();
                if (span.IsEmpty) continue;
                if (IsUrlLine(span)) { urlSeen = true; continue; }
                if (IsRecognisedOption(span)) return true;
            }
            return urlSeen;
        }
        catch (IOException) { return false; }
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

public sealed partial class UrlSanitizer : IUrlSanitizer
{
    // user:pass@ after scheme:// ; also covers ftp-style and rare cases with @ in path later
    [GeneratedRegex(@"(?<scheme>[a-zA-Z][a-zA-Z0-9+.-]*)://(?<user>[^:@/\s]+):(?<pass>[^@/\s]+)@")]
    private static partial Regex CredentialPattern();

    // Only strip controls + Spectre markup brackets. Never touch & ' " < > — they are valid in URLs.
    private static readonly SearchValues<char> MarkupChars = SearchValues.Create("[]");

    public string Sanitize(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return string.Create(value.Length, value, static (span, source) =>
        {
            var i = 0;
            foreach (var ch in source.AsSpan())
            {
                if (char.IsControl(ch) || MarkupChars.Contains(ch)) span[i++] = '?';
                else span[i++] = ch;
            }
        });
    }

    public string MaskCredentials(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return CredentialPattern().Replace(value, "${scheme}://***@");
    }
}

public sealed class UrlNameGuesser : IUrlNameGuesser
{
    private static readonly SearchValues<char> InvalidFileNameChars = SearchValues.Create(Path.GetInvalidFileNameChars());

    public string GuessName(Uri url)
    {
        if (url == null) return "download";
        try
        {
            var name = Path.GetFileName(url.LocalPath);
            if (string.IsNullOrEmpty(name)) name = url.Host;
            if (string.IsNullOrEmpty(name)) name = "download";
            return SanitizeFileName(name);
        }
        catch (InvalidOperationException) { return "download"; }
        catch (ArgumentException) { return "download"; }
    }

    private static string SanitizeFileName(string name)
    {
        var sb = new StringBuilder(name.Length);
        foreach (var ch in name)
        {
            if (InvalidFileNameChars.Contains(ch)) sb.Append('_');
            else sb.Append(ch);
        }
        return sb.ToString();
    }
}
