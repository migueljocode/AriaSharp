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

    /// <param name="autoYes">Skip interactive consent when true.</param>
    /// <param name="confirm">Optional interactive consent for a displayed command.</param>
    /// <param name="log">Optional log sink (package-manager stdout/stderr and fallback notices).</param>
    public static async Task<(bool Ok, string Message, InstallFailureKind Kind)> InstallAria2Async(
        CancellationToken ct,
        bool autoYes = true,
        Func<string, bool>? confirm = null,
        TextWriter? log = null)
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

        var notes = new List<string>();

        foreach (var mgr in PackageManagerCatalog.Aria2Managers.Where(static m => m.IsOsMatch()))
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

            var resolved = ResolveInstallCommand(mgr);
            if (resolved is null)
            {
                notes.Add($"{mgr.DisplayName}: needs root (no sudo); skipping to next option");
                if (log is not null) await log.WriteLineAsync(notes[^1]);
                continue;
            }

            var (file, args) = resolved.Value;
            if (log is not null) await log.WriteLineAsync($"running: {file} {args}");
            var install = await ProcessRunner.RunAsync(file, args, ct);
            if (log is not null) await log.WriteLineAsync(install.StdOut);
            if (log is not null) await log.WriteLineAsync(install.StdErr);

            if (install.ExitCode == 0)
            {
                try
                {
                    _ = await new Aria2Locator().LocateAsync(ct);
                    return (true, $"installed via {mgr.DisplayName}", InstallFailureKind.None);
                }
                catch (FileNotFoundException)
                {
                    notes.Add($"{mgr.DisplayName}: exit 0 but aria2c not found on PATH");
                    if (log is not null) await log.WriteLineAsync(notes[^1]);
                }
            }
            else
            {
                var kind = ClassifyManagerFailure(install.ExitCode, install.StdErr);
                notes.Add($"{mgr.DisplayName} failed ({kind}): {file} {args}");
                if (log is not null) await log.WriteLineAsync(notes[^1]);
            }
        }

        if (log is not null) await log.WriteLineAsync("falling back to pinned official artifact / source build…");
        notes.Add("falling back to pinned official artifact / source build");

        AppPaths.EnsureDataDirectories();
        var cache = Path.Join(AppPaths.ToolsCacheDirectory, $"aria2-{Guid.NewGuid():N}");
        Directory.CreateDirectory(cache);
        try
        {
            var (ok, message, logPath) = await Aria2GitHubInstaller.InstallForCurrentOsAsync(cache, ct);
            if (!ok)
            {
                var kind = message.Contains("SHA-256", StringComparison.Ordinal) ? InstallFailureKind.HashMismatch
                    : message.Contains("build tools", StringComparison.OrdinalIgnoreCase) || message.Contains("headers", StringComparison.OrdinalIgnoreCase)
                        ? InstallFailureKind.BuildTools
                        : InstallFailureKind.Unknown;
                var prefix = notes.Count > 0 ? string.Join("; ", notes) + "; " : "";
                return (false,
                    prefix + $"source/zip fallback failed: {message}"
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

    public static InstallFailureKind ClassifyManagerFailure(int exitCode, string stdErr)
    {
        ArgumentNullException.ThrowIfNull(stdErr);
        if (stdErr.Contains("lock", StringComparison.OrdinalIgnoreCase)
            || stdErr.Contains("could not get lock", StringComparison.OrdinalIgnoreCase))
            return InstallFailureKind.ManagerBusy;
        if (stdErr.Contains("permission", StringComparison.OrdinalIgnoreCase)
            || stdErr.Contains("denied", StringComparison.OrdinalIgnoreCase)
            || exitCode is 126 or 127)
            return InstallFailureKind.Auth;
        if (exitCode is 1 or 100)
            return InstallFailureKind.PackageMissing;
        return InstallFailureKind.Unknown;
    }

    /// <summary>
    /// Returns (file, args) or null when elevation is required but unavailable
    /// (caller should skip to the next manager / artifact fallback).
    /// </summary>
    public static (string File, string Args)? ResolveInstallCommand(PackageManagerEntry mgr)
    {
        ArgumentNullException.ThrowIfNull(mgr);
        if (!mgr.NeedsElevation || OperatingSystem.IsWindows())
            return (mgr.InstallFileName, mgr.InstallArgs);

        if (IsPrivileged())
            return (mgr.InstallFileName, mgr.InstallArgs);

        if (PackageManagerCatalog.CommandExists("sudo"))
            return ("sudo", $"{mgr.InstallFileName} {mgr.InstallArgs}");

        return null;
    }

    public static bool IsPrivileged()
    {
        try
        {
            return Environment.IsPrivilegedProcess;
        }
        catch (Exception ex) when (ex is PlatformNotSupportedException or InvalidOperationException)
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
            $"privileged: {IsPrivileged()}",
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
                using var log = new StringWriter();
                var (installed, msg, kind) = await InstallAria2Async(ct, autoYes: true, log: log);
                lines.Add($"aria2c: {msg} ({kind})");
                var logText = log.ToString();
                if (logText.Length > 0)
                    lines.Add(logText.TrimEnd());
                ok = installed;
            }
        }

        lines.Add("managers:");
        foreach (var mgr in PackageManagerCatalog.Aria2Managers.Where(static m => m.IsOsMatch()))
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
