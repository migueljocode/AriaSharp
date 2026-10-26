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

public sealed class UrlExtractor : IUrlExtractor
{
    private static readonly Regex UrlPattern = new(@"(?<scheme>https?|ftp|ftps|sftp|magnet):\/\/[^\s""'<>]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    public string[] Extract(string text)
    {
        if (string.IsNullOrEmpty(text)) return Array.Empty<string>();
        var list = new List<string>();
        foreach (Match m in UrlPattern.Matches(text))
        {
            var url = m.Value.TrimEnd(',', ';', ')', ']', '}', '.', '!', '?');
            if (!string.IsNullOrEmpty(url)) list.Add(url);
        }
        return list.ToArray();
    }
}

public sealed class UrlClassifier : IUrlClassifier
{
    private static readonly string[] Schemes = { "http://", "https://", "ftp://", "ftps://", "sftp://", "magnet:?" };
    public bool IsUrl(string value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        var span = value.AsSpan();
        foreach (var s in Schemes) if (span.StartsWith(s, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
    public bool IsListFile(string value) => File.Exists(value);
    public bool IsSessionFile(string value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        return Path.GetFileName(value).Equals("resume.session", StringComparison.OrdinalIgnoreCase) || value.EndsWith(".session", StringComparison.OrdinalIgnoreCase);
    }
    public bool IsAria2ListFile(string value)
    {
        if (string.IsNullOrEmpty(value) || !File.Exists(value)) return false;
        try
        {
            foreach (var line in File.ReadLines(value))
            {
                var t = line.Trim();
                if (t.Length == 0) continue;
                if (t.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || t.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || t.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase) || t.StartsWith("magnet:?", StringComparison.OrdinalIgnoreCase)) return true;
                if (t.StartsWith("dir=", StringComparison.OrdinalIgnoreCase) || t.StartsWith("out=", StringComparison.OrdinalIgnoreCase) || t.Contains('=', StringComparison.Ordinal)) return true;
            }
        }
        catch (IOException) { return false; }
        return false;
    }
}

public sealed class UrlSanitizer : IUrlSanitizer
{
    private static readonly Regex CredentialPattern = new(@"(?<scheme>[a-zA-Z][a-zA-Z0-9+.-]*)://(?<user>[^:@/]+):(?<pass>[^@/]+)@", RegexOptions.Compiled);
    public string Sanitize(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var sb = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (char.IsControl(ch) || ch is '<' or '>' or '&' or '"' or '\'') sb.Append('?');
            else sb.Append(ch);
        }
        return sb.ToString();
    }
    public string MaskCredentials(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return CredentialPattern.Replace(value, "${scheme}://***@");
    }
}

public sealed class UrlNameGuesser : IUrlNameGuesser
{
    public string GuessName(Uri url)
    {
        if (url == null) return "download";
        try
        {
            var name = Path.GetFileName(url.LocalPath);
            if (string.IsNullOrEmpty(name)) name = url.Host;
            if (string.IsNullOrEmpty(name)) name = "download";
            foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c.ToString(), "_", StringComparison.Ordinal);
            return name;
        }
        catch (Exception) { return "download"; }
    }
}