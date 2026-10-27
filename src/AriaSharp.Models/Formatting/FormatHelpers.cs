namespace AriaSharp.Models;

/// <summary>
/// Static entry points for <c>global using static</c>. Implementation lives in <see cref="FormatExtensions"/>.
/// </summary>
public static class FormatHelpers
{
    public static string FormatBytes(long bytes) => bytes.AsByteSize();
    public static string FormatSpeed(long bytesPerSec) => bytesPerSec.AsSpeed();

    public static string Sanitize(string? value) =>
        value is null ? string.Empty : value.SanitizeMarkup();

    public static string MaskCredentials(string? value) =>
        value is null ? string.Empty : value.MaskCredentialText();

    public static string Truncate(string? value, int max) =>
        value is null ? string.Empty : value.TruncateTo(max);

    public static string ProtocolBadge(DownloadProtocol protocol) => protocol.Badge();
}
