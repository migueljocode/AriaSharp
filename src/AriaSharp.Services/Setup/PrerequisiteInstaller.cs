using System.IO.Compression;


namespace AriaSharp.Services;

/// <summary>
/// Ensures aria2 is available without sudo/admin: probe PATH and user tools dir,
/// otherwise download the official GitHub release, install under
/// <see cref="AppPaths.ToolsBinDirectory"/>, and delete cache artifacts.
/// Linux/macOS build from source when no platform binary is published.
/// </summary>
public sealed class PrerequisiteInstaller : IPrerequisiteInstaller
{
    private static readonly HttpClient Http = CreateHttp();

    public async Task<bool> InstallAsync(string package, CancellationToken ct)
    {
        var logical = package switch
        {
            "aria2c" or "aria2" => "aria2",
            _ => package,
        };
        if (logical != "aria2")
            return false; // clipboard helpers: system-only, no privileged install
        return await InstallAria2Async(ct).ConfigureAwait(false);
    }

    public async Task<bool> IsInstalledAsync(string package, CancellationToken ct)
    {
        _ = ct;
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
                _ = await new Aria2Locator().LocateAsync(ct).ConfigureAwait(false);
                return true;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
        }

        if (OperatingSystem.IsWindows())
            return false;

        var result = await ProcessRunner.RunAsync("which", command, ct).ConfigureAwait(false);
        return result.ExitCode == 0;
    }

    public static async Task<bool> InstallAria2Async(CancellationToken ct)
    {
        try
        {
            _ = await new Aria2Locator().LocateAsync(ct).ConfigureAwait(false);
            return true; // already present
        }
        catch (FileNotFoundException)
        {
            // continue to bootstrap
        }

        AppPaths.EnsureDataDirectories();
        var cache = Path.Join(AppPaths.ToolsCacheDirectory, $"aria2-{Guid.NewGuid():N}");
        Directory.CreateDirectory(cache);
        try
        {
            if (OperatingSystem.IsWindows())
                return await InstallWindowsAsync(cache, ct).ConfigureAwait(false);
            return await InstallFromSourceAsync(cache, ct).ConfigureAwait(false);
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
                var existing = await new Aria2Locator().LocateAsync(ct).ConfigureAwait(false);
                lines.Add($"aria2c: already installed ({existing})");
            }
            catch (FileNotFoundException)
            {
                lines.Add($"aria2c: downloading {Aria2Manifest.Version} from GitHub…");
                var installed = await InstallAria2Async(ct).ConfigureAwait(false);
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
                var probe = await ProcessRunner.RunAsync("which", tool, ct).ConfigureAwait(false);
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

    private static async Task<bool> InstallWindowsAsync(string cache, CancellationToken ct)
    {
        var zipPath = Path.Join(cache, "aria2-win.zip");
        await DownloadAsync(Aria2Manifest.WindowsZipUrl, zipPath, ct).ConfigureAwait(false);

        var extractDir = Path.Join(cache, "extract");
        Directory.CreateDirectory(extractDir);
        await ZipFile.ExtractToDirectoryAsync(zipPath, extractDir, overwriteFiles: true, cancellationToken: ct).ConfigureAwait(false);

        var exe = Directory.EnumerateFiles(extractDir, "aria2c.exe", SearchOption.AllDirectories)
            .FirstOrDefault();
        if (exe is null)
            return false;

        var dest = AppPaths.BundledAria2cPath;
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        File.Copy(exe, dest, overwrite: true);
        return File.Exists(dest);
    }

    private static async Task<bool> InstallFromSourceAsync(string cache, CancellationToken ct)
    {
        // Prefer .tar.gz (tar handles it everywhere); try .xz if gz fails to download
        var tarball = Path.Join(cache, "aria2-src.tar.gz");
        try
        {
            await DownloadAsync(Aria2Manifest.SourceTarballGzUrl, tarball, ct).ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            tarball = Path.Join(cache, "aria2-src.tar.xz");
            await DownloadAsync(Aria2Manifest.SourceTarballUrl, tarball, ct).ConfigureAwait(false);
        }

        var extractDir = Path.Join(cache, "src");
        Directory.CreateDirectory(extractDir);
        var tar = await ProcessRunner.RunAsync(
                "tar",
                $"-xf \"{tarball}\" -C \"{extractDir}\" --strip-components=1",
                ct,
                workingDirectory: cache)
            .ConfigureAwait(false);
        if (tar.ExitCode != 0)
        {
            // retry without strip
            tar = await ProcessRunner.RunAsync("tar", $"-xf \"{tarball}\" -C \"{extractDir}\"", ct)
                .ConfigureAwait(false);
            if (tar.ExitCode != 0)
                return false;
            // find nested source root
            var nested = Directory.EnumerateDirectories(extractDir, "aria2-*").FirstOrDefault();
            if (nested is not null)
                extractDir = nested;
        }

        var prefix = Path.Join(cache, "prefix");
        Directory.CreateDirectory(prefix);

        var configure = await ProcessRunner.RunAsync(
                "/bin/sh",
                $"./configure --prefix=\"{prefix}\" --disable-nls --without-libxml2 --without-libexpat",
                ct,
                workingDirectory: extractDir)
            .ConfigureAwait(false);
        if (configure.ExitCode != 0)
        {
            // simpler configure for minimal hosts
            configure = await ProcessRunner.RunAsync(
                    "/bin/sh",
                    $"./configure --prefix=\"{prefix}\"",
                    ct,
                    workingDirectory: extractDir)
                .ConfigureAwait(false);
            if (configure.ExitCode != 0)
                return false;
        }

        var jobs = Math.Max(1, Environment.ProcessorCount);
        var make = await ProcessRunner.RunAsync("make", $"-j{jobs}", ct, workingDirectory: extractDir)
            .ConfigureAwait(false);
        if (make.ExitCode != 0)
            return false;

        var install = await ProcessRunner.RunAsync("make", "install", ct, workingDirectory: extractDir)
            .ConfigureAwait(false);
        if (install.ExitCode != 0)
            return false;

        var built = Path.Join(prefix, "bin", "aria2c");
        if (!File.Exists(built))
        {
            // sometimes sits in src tree
            built = Directory.EnumerateFiles(extractDir, "aria2c", SearchOption.AllDirectories)
                .FirstOrDefault(p => !p.Contains("/.libs/", StringComparison.Ordinal))
                ?? Path.Join(extractDir, "src", "aria2c");
        }
        if (!File.Exists(built))
            return false;

        var dest = AppPaths.BundledAria2cPath;
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        File.Copy(built, dest, overwrite: true);
        if (!OperatingSystem.IsWindows())
        {
            _ = await ProcessRunner.RunAsync("chmod", $"+x \"{dest}\"", ct).ConfigureAwait(false);
        }
        return File.Exists(dest);
    }

    private static async Task DownloadAsync(string url, string dest, CancellationToken ct)
    {
        using var resp = await Http.GetAsync(new Uri(url), HttpCompletionOption.ResponseHeadersRead, ct)
            .ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        await using (stream.ConfigureAwait(false))
        {
            var file = new FileStream(
                dest,
                new FileStreamOptions
                {
                    Mode = FileMode.Create,
                    Access = FileAccess.Write,
                    Share = FileShare.None,
                    Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
                });
            await using (file.ConfigureAwait(false))
            {
                await stream.CopyToAsync(file, ct).ConfigureAwait(false);
            }
        }
    }

    private static HttpClient CreateHttp()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        c.DefaultRequestHeaders.UserAgent.ParseAdd(AppDefaults.UserAgent);
        return c;
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
