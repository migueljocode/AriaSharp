using AriaSharp.Models;

namespace AriaSharp.Services;

/// <summary>
/// Installs aria2 and optional clipboard helpers using whichever package manager
/// is available on the host (apt/dnf/pacman/zypper/apk, brew/port, winget/choco/scoop).
/// Never builds from source (hang + privilege risk).
/// </summary>
public sealed class PrerequisiteInstaller : IPrerequisiteInstaller
{
    public async Task<bool> InstallAsync(string package, CancellationToken ct)
    {
        var logical = Normalize(package);
        return await InstallLogicalAsync(logical, ct).ConfigureAwait(false);
    }

    public async Task<bool> IsInstalledAsync(string package, CancellationToken ct)
    {
        var command = Normalize(package) switch
        {
            "wl-clipboard" => "wl-paste",
            "xclip" => "xclip",
            "xsel" => "xsel",
            "aria2" or "aria2c" => "aria2c",
            _ => package,
        };

        if (OperatingSystem.IsWindows() && command is "aria2c" or "aria2")
        {
            // where.exe returns 0 when found
            var where = await ProcessRunner.RunAsync("where", "aria2c", ct).ConfigureAwait(false);
            if (where.ExitCode == 0) return true;
            where = await ProcessRunner.RunAsync("where", "aria2", ct).ConfigureAwait(false);
            return where.ExitCode == 0;
        }

        var result = await ProcessRunner.RunAsync(WhichCommand(), command, ct).ConfigureAwait(false);
        return result.ExitCode == 0;
    }

    public static async Task<bool> InstallAria2Async(CancellationToken ct) =>
        await InstallLogicalAsync("aria2", ct).ConfigureAwait(false);

    public static async Task<(bool Ok, string Message)> InstallDepsAsync(bool installAria2, CancellationToken ct)
    {
        var messages = new List<string>();
        var ok = true;
        var installer = new PrerequisiteInstaller();
        var manager = await DetectManagerAsync(ct).ConfigureAwait(false);

        messages.Add(manager is null
            ? $"platform: {DescribeOs()} (no package manager detected)"
            : $"platform: {DescribeOs()} · manager: {manager.Id}");

        if (installAria2)
        {
            if (await installer.IsInstalledAsync("aria2c", ct).ConfigureAwait(false))
            {
                messages.Add("aria2c: already installed");
            }
            else
            {
                var installed = await InstallLogicalAsync("aria2", ct).ConfigureAwait(false);
                if (installed)
                    messages.Add("aria2: installed");
                else
                {
                    messages.Add("aria2: install failed (need a package manager + privileges)");
                    ok = false;
                }
            }
        }

        // Clipboard helpers: Linux only (macOS/Windows have built-in clipboard APIs/tools)
        if (OperatingSystem.IsLinux())
        {
            foreach (var pkg in new[] { "wl-clipboard", "xclip", "xsel" })
            {
                if (await installer.IsInstalledAsync(pkg, ct).ConfigureAwait(false))
                {
                    messages.Add($"{pkg}: already installed");
                    continue;
                }
                var installed = await InstallLogicalAsync(pkg, ct).ConfigureAwait(false);
                messages.Add(installed ? $"{pkg}: installed" : $"{pkg}: skipped/failed");
            }
        }
        else if (OperatingSystem.IsMacOS())
        {
            messages.Add("clipboard: pbpaste (system)");
        }
        else if (OperatingSystem.IsWindows())
        {
            messages.Add("clipboard: PowerShell Get-Clipboard (system)");
        }

        return (ok, string.Join(Environment.NewLine, messages));
    }

    private static async Task<bool> InstallLogicalAsync(string logical, CancellationToken ct)
    {
        var manager = await DetectManagerAsync(ct).ConfigureAwait(false);
        if (manager is null)
            return false;

        var pkg = PackageManagerCatalog.MapPackage(logical, manager.Id);
        // Skip clipboard packages on managers that don't list them
        if (logical is not ("aria2" or "aria2c") &&
            !manager.Packages.Any(p => p.Equals(pkg, StringComparison.OrdinalIgnoreCase)
                || p.Equals(logical, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        var args = string.Format(System.Globalization.CultureInfo.InvariantCulture, manager.InstallArgsFormat, pkg);
        var result = await ProcessRunner.RunAsync(manager.Binary, args, ct).ConfigureAwait(false);
        return result.ExitCode == 0;
    }

    private static async Task<PackageManagerCatalog.Manager?> DetectManagerAsync(CancellationToken ct)
    {
        IReadOnlyList<PackageManagerCatalog.Manager> candidates =
            OperatingSystem.IsWindows() ? PackageManagerCatalog.WindowsManagers
            : OperatingSystem.IsMacOS() ? PackageManagerCatalog.MacManagers
            : PackageManagerCatalog.LinuxManagers;

        foreach (var m in candidates)
        {
            var probe = await ProcessRunner.RunAsync(WhichCommand(), m.Binary, ct).ConfigureAwait(false);
            if (probe.ExitCode == 0)
                return m;
            // Windows: winget/choco may not be on PATH for `where` in some shells — try bare start
            if (OperatingSystem.IsWindows())
            {
                var direct = await ProcessRunner.RunAsync(m.Binary, "--version", ct).ConfigureAwait(false);
                if (direct.ExitCode == 0 || !string.IsNullOrEmpty(direct.StdOut) || !string.IsNullOrEmpty(direct.StdErr))
                    return m;
            }
        }
        return null;
    }

    private static string Normalize(string package) => package switch
    {
        "aria2c" => "aria2",
        "wl-clipboard" => "wl-clipboard",
        _ => package,
    };

    private static string WhichCommand() => OperatingSystem.IsWindows() ? "where" : "which";

    private static string DescribeOs()
    {
        if (OperatingSystem.IsWindows()) return "Windows";
        if (OperatingSystem.IsMacOS()) return "macOS";
        if (OperatingSystem.IsLinux()) return "Linux";
        return "unknown";
    }
}
