namespace AriaSharp.Models;

/// <summary>C# 14 extension members for formatting helpers.</summary>
public static class FormatExtensions
{
    extension(long bytes)
    {
        public string AsByteSize() => FormatHelpers.FormatBytes(bytes);

        public string AsSpeed() => FormatHelpers.FormatSpeed(bytes);
    }

    extension(string? value)
    {
        public string SanitizeMarkup() => FormatHelpers.Sanitize(value);

        public string MaskCredentialText() => FormatHelpers.MaskCredentials(value);

        public string TruncateTo(int max) => FormatHelpers.Truncate(value, max);
    }

    extension(DownloadProtocol protocol)
    {
        public string Badge() => FormatHelpers.ProtocolBadge(protocol);
    }
}
