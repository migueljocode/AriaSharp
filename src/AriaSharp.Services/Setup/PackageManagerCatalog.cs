namespace AriaSharp.Services;

/// <summary>
/// Host package-manager probes and package name maps per OS.
/// </summary>
public static class PackageManagerCatalog
{
    internal sealed record Manager(string Id, string Binary, string InstallArgsFormat, IReadOnlyList<string> Packages);

    public static readonly string[] LinuxClipboardTools = ["wl-paste", "xclip", "xsel"];

    internal static IReadOnlyList<Manager> LinuxManagers { get; } =
    [
        new("apt", "apt-get", "install -y {0}", ["aria2", "wl-clipboard", "xclip", "xsel"]),
        new("dnf", "dnf", "install -y {0}", ["aria2", "wl-clipboard", "xclip", "xsel"]),
        new("yum", "yum", "install -y {0}", ["aria2", "xclip", "xsel"]),
        new("pacman", "pacman", "-S --noconfirm {0}", ["aria2", "wl-clipboard", "xclip", "xsel"]),
        new("zypper", "zypper", "install -y {0}", ["aria2", "xclip", "xsel"]),
        new("apk", "apk", "add --no-cache {0}", ["aria2", "xclip", "xsel"]),
    ];

    internal static IReadOnlyList<Manager> MacManagers { get; } =
    [
        new("brew", "brew", "install {0}", ["aria2"]),
        new("port", "port", "install {0}", ["aria2"]),
    ];

    internal static IReadOnlyList<Manager> WindowsManagers { get; } =
    [
        new("winget", "winget", "install -e --id {0} --accept-package-agreements --accept-source-agreements", ["aria2.aria2"]),
        new("choco", "choco", "install {0} -y", ["aria2"]),
        new("scoop", "scoop", "install {0}", ["aria2"]),
    ];

    public static string MapPackage(string logical, string managerId) => (logical, managerId) switch
    {
        ("aria2" or "aria2c", "winget") => "aria2.aria2",
        ("aria2" or "aria2c", _) => "aria2",
        ("wl-clipboard", _) => "wl-clipboard",
        ("xclip", _) => "xclip",
        ("xsel", _) => "xsel",
        _ => logical,
    };
}

public static class Aria2Manifest
{
    public const string Version = "1.37.0";
    public const string WindowsZipUrl =
        "https://github.com/aria2/aria2/releases/download/release-1.37.0/aria2-1.37.0-win-64bit-build1.zip";
    public const string SourceTarballUrl =
        "https://github.com/aria2/aria2/releases/download/release-1.37.0/aria2-1.37.0.tar.gz";
}
