namespace AriaSharp.Services;

/// <summary>
/// Ensures aria2 is available without sudo/admin: probe PATH and user tools dir,
/// otherwise download the official GitHub release via <see cref="Aria2GitHubInstaller"/>.
/// </summary>
public sealed class PrerequisiteInstaller : IPrerequisiteInstaller
{
    public async Task<bool> InstallAsync(string package, CancellationToken ct)
    {
        var logical = package switch
        {
            "aria2c" or "aria2" => "aria2",
            _ => package,
        };
        if (logical != "aria2")
            return false;
        return await InstallAria2Async(ct);
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

    public static async Task<bool> InstallAria2Async(CancellationToken ct)
    {
        try
        {
            _ = await new Aria2Locator().LocateAsync(ct);
            return true;
        }
        catch (FileNotFoundException) { }

        AppPaths.EnsureDataDirectories();
        var cache = Path.Join(AppPaths.ToolsCacheDirectory, $"aria2-{Guid.NewGuid():N}");
        Directory.CreateDirectory(cache);
        try
        {
            return await Aria2GitHubInstaller.InstallForCurrentOsAsync(cache, ct);
        }
        finally
        {
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
                lines.Add($"aria2c: downloading {Aria2Manifest.Version} from GitHub…");
                var installed = await InstallAria2Async(ct);
                if (installed && File.Exists(AppPaths.BundledAria2cPath))
                    lines.Add($"aria2c: installed → {AppPaths.BundledAria2cPath}");
                else if (installed)
                    lines.Add("aria2c: installed (system PATH)");
                else
                {
                    lines.Add("aria2c: install failed (network or build tools missing)");
                    ok = false;
                }
            }
        }

        if (OperatingSystem.IsLinux() && HasGuiSession())
        {
            foreach (var (pkg, tool) in new[] { ("wl-clipboard", "wl-paste"), ("xclip", "xclip"), ("xsel", "xsel") })
            {
                var probe = await ProcessRunner.RunAsync("which", tool, ct);
                lines.Add(probe.ExitCode == 0
                    ? $"{pkg}: available"
                    : $"{pkg}: not found (optional; install via your package manager if needed)");
            }
        }
        else if (OperatingSystem.IsMacOS())
            lines.Add("clipboard: pbpaste (system)");
        else if (OperatingSystem.IsWindows())
            lines.Add("clipboard: PowerShell Get-Clipboard (system)");
        else
            lines.Add("clipboard: skipped (no GUI session or unsupported OS)");

        return (ok, string.Join(Environment.NewLine, lines));
    }

    private static bool HasGuiSession() =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY"))
        || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"));

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
