namespace AriaSharp.Services;

/// <summary>Official aria2 1.37.0 artifacts. Upstream publishes no checksums; we pin SHA-256.</summary>
public static class Aria2Manifest
{
    public const string Version = "1.37.0";
    public const string ReleaseTag = "release-1.37.0";

    public const string WindowsZipUrl =
        "https://github.com/aria2/aria2/releases/download/release-1.37.0/aria2-1.37.0-win-64bit-build1.zip";

    /// <summary>Primary source archive (prefer .tar.gz; no .xz fallback).</summary>
    public const string SourceTarballGzUrl =
        "https://github.com/aria2/aria2/releases/download/release-1.37.0/aria2-1.37.0.tar.gz";

    /// <summary>SHA-256 of the win-64bit zip (computed locally; re-verify if the URL changes).</summary>
    public const string WindowsZipSha256 =
        "67d015301eef0b612191212d564c5bb0a14b5b9c4796b76454276a4d28d9b288";

    /// <summary>SHA-256 of aria2-1.37.0.tar.gz.</summary>
    public const string SourceTarballGzSha256 =
        "8e7021c6d5e8f8240c9cc19482e0c8589540836747744724d86bf8af5a21f0e8";
}

/// <summary>One package-manager entry for probing/installing aria2.</summary>
public sealed record PackageManagerEntry(
    string Id,
    string DisplayName,
    string ProbeArgs,
    string InstallArgs,
    bool NeedsElevation,
    Func<bool> IsOsMatch);

/// <summary>Data-driven package managers that may provide aria2.</summary>
public static class PackageManagerCatalog
{
    public static IReadOnlyList<PackageManagerEntry> Aria2Managers { get; } =
    [
        new("apt", "apt", "cache policy aria2", "install -y aria2", true, static () => File.Exists("/usr/bin/apt-get")),
        new("dnf", "dnf", "list --available aria2", "install -y aria2", true, static () => File.Exists("/usr/bin/dnf")),
        new("zypper", "zypper", "search -x aria2", "install -y aria2", true, static () => File.Exists("/usr/bin/zypper")),
        new("pacman", "pacman", "-Si aria2", "-S --noconfirm aria2", true, static () => File.Exists("/usr/bin/pacman")),
        new("apk", "apk", "search -e aria2", "add aria2", true, static () => File.Exists("/sbin/apk") || File.Exists("/usr/bin/apk")),
        new("brew", "brew", "info aria2", "install aria2", false, static () => OperatingSystem.IsMacOS() || File.Exists("/home/linuxbrew/.linuxbrew/bin/brew")),
        new("winget", "winget", "show --id aria2.aria2", "install -e --id aria2.aria2 --accept-package-agreements --accept-source-agreements", false, static () => OperatingSystem.IsWindows()),
        new("choco", "choco", "search aria2 --exact --limit-output", "install aria2 -y", true, static () => OperatingSystem.IsWindows()),
        new("scoop", "scoop", "info aria2", "install aria2", false, static () => OperatingSystem.IsWindows()),
    ];
}
