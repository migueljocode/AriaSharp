using System.Buffers;

namespace AriaSharp.Models;

/// <summary>C# 14 extension members — canonical implementation for formatting.</summary>
public static class FormatExtensions
{
    extension(long bytes)
    {
        public string AsByteSize()
        {
            if (bytes < 0) return "--";
            const long k = 1024;
            return bytes switch
            {
                < k => $"{bytes} B",
                < k * k => $"{bytes / (double)k:F1} KB",
                < k * k * k => $"{bytes / (double)(k * k):F1} MB",
                < k * k * k * k => $"{bytes / (double)(k * k * k):F1} GB",
                _ => $"{bytes / (double)(k * k * k * k):F2} TB",
            };
        }

        public string AsSpeed() => bytes < 0 ? "--" : bytes.AsByteSize() + "/s";
    }

    extension(string? value)
    {
        public string SanitizeMarkup()
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

        public string MaskCredentialText()
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

        public string TruncateTo(int max)
        {
            if (value is null || value.Length == 0) return string.Empty;
            if (value.Length <= max) return value;
            return max <= 3 ? value[..max] : value[..(max - 3)] + "...";
        }
    }

    extension(DownloadProtocol protocol)
    {
        public string Badge() => protocol switch
        {
            DownloadProtocol.Https => "HTTPS",
            DownloadProtocol.Http => "HTTP",
            DownloadProtocol.Ftp => "FTP",
            DownloadProtocol.Ftps => "FTPS",
            DownloadProtocol.Sftp => "SFTP",
            DownloadProtocol.Magnet => "MAGNET",
            _ => "----",
        };
    }
}
