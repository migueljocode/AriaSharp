using System.Buffers;
using System.Text;

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
        if (string.IsNullOrEmpty(value)) return string.Empty;
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
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var schemeEnd = value.IndexOf("://", StringComparison.Ordinal);
        if (schemeEnd < 0) return value;
        var at = value[(schemeEnd + 3)..].IndexOf('@');
        if (at < 0) return value;
        return value[..(schemeEnd + 3)] + "***@" + value[(schemeEnd + 4 + at)..];
    }

    public static string Truncate(string? value, int max)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
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