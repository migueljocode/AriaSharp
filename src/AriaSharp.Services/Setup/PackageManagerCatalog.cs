namespace AriaSharp.Services;

/// <summary>
/// Official aria2 release artifacts (1.37.0). No published checksums upstream.
/// </summary>
public static class Aria2Manifest
{
    public const string Version = "1.37.0";
    public const string ReleaseTag = "release-1.37.0";

    public const string WindowsZipUrl =
        "https://github.com/aria2/aria2/releases/download/release-1.37.0/aria2-1.37.0-win-64bit-build1.zip";

    public const string SourceTarballUrl =
        "https://github.com/aria2/aria2/releases/download/release-1.37.0/aria2-1.37.0.tar.xz";

    /// <summary>Fallback if .xz tools missing.</summary>
    public const string SourceTarballGzUrl =
        "https://github.com/aria2/aria2/releases/download/release-1.37.0/aria2-1.37.0.tar.gz";
}
