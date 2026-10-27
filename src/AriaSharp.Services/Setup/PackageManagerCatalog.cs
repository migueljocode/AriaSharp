namespace AriaSharp.Services;

/// <summary>Official aria2 1.37.0 artifacts. Upstream publishes no checksums; we pin SHA-256.</summary>
public static class Aria2Manifest
{
    public const string Version = AppDefaults.Aria2Version;
    public const string ReleaseTag = AppDefaults.Aria2ReleaseTag;

    public const string WindowsZipUrl64 =
        "https://github.com/aria2/aria2/releases/download/" + ReleaseTag + "/aria2-" + Version + "-win-64bit-build1.zip";

    public const string WindowsZipUrl32 =
        "https://github.com/aria2/aria2/releases/download/" + ReleaseTag + "/aria2-" + Version + "-win-32bit-build1.zip";

    public const string SourceTarballGzUrl =
        "https://github.com/aria2/aria2/releases/download/" + ReleaseTag + "/aria2-" + Version + ".tar.gz";

    public const string WindowsZipSha256X64 =
        "67d015301eef0b612191212d564c5bb0a14b5b9c4796b76454276a4d28d9b288";

    public const string WindowsZipSha256X86 =
        "35f6514cc5dd7e98a87b3c4c2d25a0754b9b063dbe59bc0f22d483464f61e5b6";

    public const string SourceTarballGzSha256 =
        "8e7021c6d5e8f8240c9cc19482e0c8589540836747744724d86bf8af5a21f0e8";

    public static (string Url, string Sha256) WindowsArtifact()
    {
        var arch = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture;
        return arch is System.Runtime.InteropServices.Architecture.X86 or System.Runtime.InteropServices.Architecture.Arm
            ? (WindowsZipUrl32, WindowsZipSha256X86)
            : (WindowsZipUrl64, WindowsZipSha256X64);
    }
}

public enum PackageProbeKind
{
    ExitCodeZero,
    AptCandidate,
    NonEmptyOutput,
}

/// <summary>One package-manager entry for probing/installing aria2.</summary>
public sealed record PackageManagerEntry(
    string Id,
    string DisplayName,
    string ProbeFileName,
    string ProbeArgs,
    string InstallFileName,
    string InstallArgs,
    bool NeedsElevation,
    PackageProbeKind ProbeKind,
    Func<bool> IsOsMatch);

/// <summary>Data-driven package managers + pure probe parsers (unit-testable).</summary>
public static class PackageManagerCatalog
{
    public static IReadOnlyList<PackageManagerEntry> Aria2Managers { get; } =
    [
        new("apt", "apt", "apt-cache", "policy aria2", "apt-get", "install -y aria2", true, PackageProbeKind.AptCandidate,
            static () => CommandExists("apt-cache") && CommandExists("apt-get")),
        new("dnf", "dnf", "dnf", "list --available aria2", "dnf", "install -y aria2", true, PackageProbeKind.ExitCodeZero,
            static () => CommandExists("dnf")),
        new("zypper", "zypper", "zypper", "search -x aria2", "zypper", "install -y aria2", true, PackageProbeKind.ExitCodeZero,
            static () => CommandExists("zypper")),
        new("pacman", "pacman", "pacman", "-Si aria2", "pacman", "-S --noconfirm aria2", true, PackageProbeKind.ExitCodeZero,
            static () => CommandExists("pacman")),
        new("apk", "apk", "apk", "search -e aria2", "apk", "add aria2", true, PackageProbeKind.NonEmptyOutput,
            static () => CommandExists("apk")),
        new("brew", "brew", "brew", "info aria2", "brew", "install aria2", false, PackageProbeKind.ExitCodeZero,
            static () => CommandExists("brew")),
        new("winget", "winget", "winget", "show --id aria2.aria2", "winget",
            "install -e --id aria2.aria2 --accept-package-agreements --accept-source-agreements", false,
            PackageProbeKind.ExitCodeZero, static () => OperatingSystem.IsWindows() && CommandExists("winget")),
        new("choco", "choco", "choco", "search aria2 --exact --limit-output", "choco", "install aria2 -y", true,
            PackageProbeKind.NonEmptyOutput, static () => OperatingSystem.IsWindows() && CommandExists("choco")),
        new("scoop", "scoop", "scoop", "info aria2", "scoop", "install aria2", false, PackageProbeKind.ExitCodeZero,
            static () => OperatingSystem.IsWindows() && CommandExists("scoop")),
    ];

    public static bool CommandExists(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;
        try
        {
            var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
            var parts = pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
            var extensions = OperatingSystem.IsWindows()
                ? (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.CMD;.BAT").Split(';', StringSplitOptions.RemoveEmptyEntries)
                : [""];
            foreach (var dir in parts)
            {
                foreach (var ext in extensions)
                {
                    var candidate = Path.Join(dir, name + ext);
                    if (File.Exists(candidate))
                        return true;
                }
                if (!OperatingSystem.IsWindows() && File.Exists(Path.Join(dir, name)))
                    return true;
            }
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException) { return false; }
        return false;
    }

    /// <summary>Pure parse: does probe output mean the package is available?</summary>
    public static bool IsPackageAvailable(PackageProbeKind kind, int exitCode, string stdOut, string stdErr)
    {
        var text = string.IsNullOrEmpty(stdOut) ? stdErr : stdOut;
        return kind switch
        {
            PackageProbeKind.ExitCodeZero => exitCode == 0,
            PackageProbeKind.AptCandidate => HasAptCandidate(text),
            PackageProbeKind.NonEmptyOutput => exitCode == 0 && !string.IsNullOrWhiteSpace(text),
            _ => false,
        };
    }

    public static bool HasAptCandidate(string output)
    {
        if (string.IsNullOrEmpty(output))
            return false;
        foreach (var line in output.Split('\n'))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith("Candidate:", StringComparison.OrdinalIgnoreCase))
                continue;
            var value = trimmed["Candidate:".Length..].Trim();
            if (value.Length == 0 || value.Equals("(none)", StringComparison.OrdinalIgnoreCase))
                return false;
            return true;
        }
        return false;
    }
}
