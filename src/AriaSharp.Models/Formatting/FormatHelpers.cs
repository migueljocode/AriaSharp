using System.Buffers;

namespace AriaSharp.Models;

public static class FormatHelpers
{
    public static string FormatBytes(long bytes)
    {
        if (bytes < 0) return "--";
        const long k = 1024;
        return bytes switch
        {
            < k => $"{bytes} B",
            < k * k => $"{bytes / (double)k:F1} KB",
            < k * k * k => $"{bytes / (double)(k * k):F1} MB",
            < k * k * k * k => $"{bytes / (double)(k * k * k):F1} GB",
            _ => $"{bytes / (double)(k * k * k * k):F2} TB"
        };
    }

    public static string FormatSpeed(long bytesPerSec) => bytesPerSec < 0 ? "--" : FormatBytes(bytesPerSec) + "/s";

    public static string Sanitize(string? value)
    {
        if (value is null || value.Length == 0) return string.Empty;
        var span = value.AsSpan();
        var rent = ArrayPool<char>.Shared.Rent(span.Length);
        try
        {
            var written = 0;
            foreach (var ch in span)
            {
                if (char.IsControl(ch) || ch is '<' or '>' or '&' or '"' or '\'')
                {
                    if (written < rent.Length) rent[written++] = '?';
                }
                else if (written < rent.Length)
                {
                    rent[written++] = ch;
                }
            }
            return new string(rent[..written]);
        }
        finally { ArrayPool<char>.Shared.Return(rent); }
    }

    public static string MaskCredentials(string? value)
    {
        if (value is null || value.Length == 0) return string.Empty;
        var schemeEnd = value.IndexOf("://", StringComparison.Ordinal);
        if (schemeEnd < 0) return value;
        var start = schemeEnd + 3;
        var rest = value.AsSpan(start);
        var end = rest.IndexOfAny("/?# ");
        var at = (end < 0 ? rest : rest[..end]).LastIndexOf('@');
        return at < 0 ? value : string.Concat(value.AsSpan(0, start), "***@", rest[(at + 1)..]);
    }

    public static string Truncate(string? value, int max)
    {
        if (value is null || value.Length == 0) return string.Empty;
        if (value.Length <= max) return value;
        return max <= 3 ? value[..max] : value[..(max - 3)] + "...";
    }

    public static string ProtocolBadge(DownloadProtocol protocol) => protocol switch
    {
        DownloadProtocol.Https => "HTTPS",
        DownloadProtocol.Http => "HTTP",
        DownloadProtocol.Ftp => "FTP",
        DownloadProtocol.Ftps => "FTPS",
        DownloadProtocol.Sftp => "SFTP",
        DownloadProtocol.Magnet => "MAGNET",
        _ => "----"
    };
}