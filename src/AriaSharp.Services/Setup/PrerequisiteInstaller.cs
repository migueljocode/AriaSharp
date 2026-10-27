namespace AriaSharp.Services;

/// <summary>
/// Install aria2: package manager when available, else pinned GitHub artifact / source build.
/// </summary>
public sealed class PrerequisiteInstaller : IPrerequisiteInstaller
{
    public Task<bool> InstallAsync(string package, CancellationToken ct)
    {
        var logical = package is "aria2c" or "aria2" ? "aria2" : package;
        return logical != "aria2" ? Task.FromResult(false) : InstallAria2Async(ct, autoYes: true);
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

    /// <param name="autoYes">When true, skip y/N for package manager installs.</param>
    public static async Task<bool> InstallAria2Async(CancellationToken ct, bool autoYes = true)
    {
        try
        {
            _ = await new Aria2Locator().LocateAsync(ct);
            return true;
        }
        catch (FileNotFoundException) { }

        var (netOk, netDetail) = await NetworkProbe.CheckAsync(ct);
        if (!netOk)
            throw new InvalidOperationException($"Cannot install aria2: {netDetail}");

        // Try package managers that both exist and list aria2.
        foreach (var mgr in PackageManagerCatalog.Aria2Managers.Where(m => m.IsOsMatch()))
        {
            var probe = await ProcessRunner.RunAsync(mgr.Id, mgr.ProbeArgs, ct);
            if (probe.ExitCode != 0)
                continue;

            if (!autoYes)
                return false; // Console layer should prompt; here default autoYes for -y paths

            var install = await ProcessRunner.RunAsync(
                mgr.NeedsElevation && !OperatingSystem.IsWindows() ? "sudo" : mgr.Id,
                mgr.NeedsElevation && !OperatingSystem.IsWindows()
                    ? $"{mgr.Id} {mgr.InstallArgs}"
                    : mgr.InstallArgs,
                ct);
            if (install.ExitCode == 0)
            {
                try
                {
                    _ = await new Aria2Locator().LocateAsync(ct);
                    return true;
                }
                catch (FileNotFoundException) { }
            }
        }

        AppPaths.EnsureDataDirectories();
        var cache = Path.Join(AppPaths.ToolsCacheDirectory, $"aria2-{Guid.NewGuid():N}");
        Directory.CreateDirectory(cache);
        try
        {
            var (ok, message, logPath) = await Aria2GitHubInstaller.InstallForCurrentOsAsync(cache, ct);
            if (!ok)
                throw new InvalidOperationException(
                    $"aria2 install failed: {message}"
                    + (logPath is not null ? $" (log: {logPath})" : ""));
            return true;
        }
        finally
        {
            // Keep log if under tools cache name install.log — tree delete removes cache after copy
            TryDeleteTree(cache);
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
                try
                {
                    var installed = await InstallAria2Async(ct, autoYes: true);
                    if (installed)
                    {
                        try
                        {
                            var path = await new Aria2Locator().LocateAsync(ct);
                            lines.Add($"aria2c: installed → {path}");
                        }
                        catch (FileNotFoundException)
                        {
                            lines.Add("aria2c: install reported ok but not on PATH");
                            ok = false;
                        }
                    }
                    else
                    {
                        lines.Add("aria2c: install failed");
                        ok = false;
                    }
                }
                catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or IOException)
                {
                    lines.Add($"aria2c: {ex.Message}");
                    ok = false;
                }
            }
        }

        lines.Add("managers:");
        foreach (var mgr in PackageManagerCatalog.Aria2Managers.Where(m => m.IsOsMatch()))
        {
            var probe = await ProcessRunner.RunAsync(mgr.Id, mgr.ProbeArgs, ct);
            lines.Add($"  {mgr.DisplayName}: {(probe.ExitCode == 0 ? "package available" : "not available")}");
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
