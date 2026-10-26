namespace AriaSharp.Services;

/// <summary>
/// Clipboard helper package names by platform (data for installers / probes).
/// </summary>
public static class PackageManagerCatalog
{
    public static readonly string[] ClipboardPackages = ["wl-clipboard", "xclip", "xsel"];
}

public static class Aria2Manifest
{
    public const string Version = "1.37.0";
    // Official GitHub release assets (verified names for 1.37.0).
    public const string WindowsZipUrl =
        "https://github.com/aria2/aria2/releases/download/release-1.37.0/aria2-1.37.0-win-64bit-build1.zip";
    public const string SourceTarballUrl =
        "https://github.com/aria2/aria2/releases/download/release-1.37.0/aria2-1.37.0.tar.gz";
}
