namespace AriaSharp.Services;

public enum InstallFailureKind
{
    None,
    Network,
    PackageMissing,
    ManagerBusy,
    Auth,
    BuildTools,
    HashMismatch,
    Unknown,
}

/// <summary>
/// Install aria2: package manager when available, else pinned GitHub artifact / source build.
/// </summary>
public sealed class PrerequisiteInstaller : IPrerequisiteInstaller
{
    public async Task<bool> InstallAsync(string package, CancellationToken ct)
    {
        var logical = package is "aria2c" or "aria2" ? "aria2" : package;
        if (logical != "aria2")
            return false;
        var (ok, _, _) = await InstallAria2Async(ct, autoYes: true);
        return ok;
    }

    public async Task<bool> IsInstalledAsync(string package, CancellationToken ct)
    {
        var command = package switch
        {
            "wl-clipboard" => "wl-paste",
            "xclip" => "xclip",
            "xsel" => "xsel",
            "aria2" or "aria2c" => "aria2c",
            _ => package,
        };

        if (command is "aria2c" or "aria2")
        {
            try
            {
                _ = await new Aria2Locator().LocateAsync(ct);
                return true;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
        }

        if (OperatingSystem.IsWindows())
            return false;

        var result = await ProcessRunner.RunAsync("which", command, ct);
        return result.ExitCode == 0;
    }

    public static async Task<(bool Ok, string Message, InstallFailureKind Kind)> InstallAria2Async(
        CancellationToken ct,
        bool autoYes = true,
        Func<string, bool>? confirm = null)
    {
        try
        {
            _ = await new Aria2Locator().LocateAsync(ct);
            return (true, "already installed", InstallFailureKind.None);
        }
        catch (FileNotFoundException) { }

        var (netOk, netDetail) = await NetworkProbe.CheckAsync(ct);
        if (!netOk)
            return (false, $"Cannot install aria2: {netDetail}", InstallFailureKind.Network);

        foreach (var mgr in PackageManagerCatalog.Aria2Managers.Where(m => m.IsOsMatch()))
        {
            ProcessRunner.Result probe;
            try
            {
                probe = await ProcessRunner.RunAsync(mgr.ProbeFileName, mgr.ProbeArgs, ct);
            }
            catch (Exception ex) when (ex is InvalidOperationException or IOException or System.ComponentModel.Win32Exception)
            {
                continue;
            }

            if (!PackageManagerCatalog.IsPackageAvailable(mgr.ProbeKind, probe.ExitCode, probe.StdOut, probe.StdErr))
                continue;

            var cmdDisplay = $"{mgr.InstallFileName} {mgr.InstallArgs}";
            if (!autoYes)
            {
                if (confirm is null || !confirm(cmdDisplay))
                    return (false, $"Refused install via {mgr.DisplayName}. Re-run with -y or run: {cmdDisplay}", InstallFailureKind.Auth);
            }

            var (file, args, elevHint) = ResolveInstallCommand(mgr);
            if (elevHint is not null)
                return (false, elevHint, InstallFailureKind.Auth);

            var install = await ProcessRunner.RunAsync(file, args, ct);
            if (install.ExitCode == 0)
            {
                try
                {
                    _ = await new Aria2Locator().LocateAsync(ct);
                    return (true, $"installed via {mgr.DisplayName}", InstallFailureKind.None);
                }
                catch (FileNotFoundException)
                {
                    // fall through
                }
            }
            else
            {
                var kind = install.ExitCode is 1 or 100 ? InstallFailureKind.PackageMissing
                    : install.StdErr.Contains("lock", StringComparison.OrdinalIgnoreCase) ? InstallFailureKind.ManagerBusy
                    : install.StdErr.Contains("permission", StringComparison.OrdinalIgnoreCase) ? InstallFailureKind.Auth
                    : InstallFailureKind.Unknown;
                // try next manager / fallback
                _ = kind;
            }
        }

        // Fallback: pinned GitHub
        AppPaths.EnsureDataDirectories();
        var cache = Path.Join(AppPaths.ToolsCacheDirectory, $"aria2-{Guid.NewGuid():N}");
        Directory.CreateDirectory(cache);
        try
        {
            var (ok, message, logPath) = await Aria2GitHubInstaller.InstallForCurrentOsAsync(cache, ct);
            if (!ok)
            {
                var kind = message.Contains("SHA-256", StringComparison.Ordinal) ? InstallFailureKind.HashMismatch
                    : message.Contains("build tools", StringComparison.OrdinalIgnoreCase) ? InstallFailureKind.BuildTools
                    : InstallFailureKind.Unknown;
                return (false,
                    $"package managers unavailable or failed; source/zip fallback failed: {message}"
                    + (logPath is not null ? $" (log: {logPath})" : "")
                    + $" — manual: install aria2 {Aria2Manifest.Version} or set --aria2-path",
                    kind);
            }
            return (true, "installed from official GitHub artifact", InstallFailureKind.None);
        }
        finally
        {
            TryDeleteTree(cache);
        }
    }

    private static (string File, string Args, string? ElevHint) ResolveInstallCommand(PackageManagerEntry mgr)
    {
        if (!mgr.NeedsElevation || OperatingSystem.IsWindows())
            return (mgr.InstallFileName, mgr.InstallArgs, null);

        if (IsRoot())
            return (mgr.InstallFileName, mgr.InstallArgs, null);

        if (PackageManagerCatalog.CommandExists("sudo"))
            return ("sudo", $"{mgr.InstallFileName} {mgr.InstallArgs}", null);

        return ("", "", $"needs root to run: {mgr.InstallFileName} {mgr.InstallArgs}");
    }

    private static bool IsRoot()
    {
        if (OperatingSystem.IsWindows())
            return false;
        try
        {
            return Environment.GetEnvironmentVariable("USER") == "root"
                || Environment.GetEnvironmentVariable("EUID") == "0"
                || Environment.UserName == "root";
        }
        catch (Exception ex) when (ex is InvalidOperationException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    public static async Task<(bool Ok, string Message)> InstallDepsAsync(bool installAria2, CancellationToken ct)
    {
        var lines = new List<string>
        {
            $"platform: {DescribeOs()}",
            $"tools: {AppPaths.ToolsBinDirectory}",
        };
        var (netOk, netDetail) = await NetworkProbe.CheckAsync(ct);
        lines.Add($"network: {(netOk ? "ok" : "fail")} ({netDetail})");

        var ok = true;
        if (installAria2)
        {
            try
            {
                var existing = await new Aria2Locator().LocateAsync(ct);
                lines.Add($"aria2c: already installed ({existing})");
            }
            catch (FileNotFoundException)
            {
                lines.Add($"aria2c: installing {Aria2Manifest.Version}…");
                var (installed, msg, _) = await InstallAria2Async(ct, autoYes: true);
                lines.Add($"aria2c: {msg}");
                ok = installed;
            }
        }

        lines.Add("managers:");
        foreach (var mgr in PackageManagerCatalog.Aria2Managers.Where(m => m.IsOsMatch()))
        {
            try
            {
                var probe = await ProcessRunner.RunAsync(mgr.ProbeFileName, mgr.ProbeArgs, ct);
                var avail = PackageManagerCatalog.IsPackageAvailable(mgr.ProbeKind, probe.ExitCode, probe.StdOut, probe.StdErr);
                lines.Add($"  {mgr.DisplayName}: {(avail ? "package available" : "not available")}");
            }
            catch (Exception ex) when (ex is InvalidOperationException or IOException or System.ComponentModel.Win32Exception)
            {
                lines.Add($"  {mgr.DisplayName}: probe failed");
            }
        }

        return (ok, string.Join(Environment.NewLine, lines));
    }

    private static string DescribeOs()
    {
        if (OperatingSystem.IsWindows()) return "Windows";
        if (OperatingSystem.IsMacOS()) return "macOS";
        if (OperatingSystem.IsLinux()) return "Linux";
        return "unknown";
    }

    private static void TryDeleteTree(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
