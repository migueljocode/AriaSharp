using System.IO.Compression;

namespace AriaSharp.Services;

/// <summary>Download and place aria2 from official GitHub release artifacts.</summary>
internal static class Aria2GitHubInstaller
{
    private static readonly HttpClient Http = CreateHttp();

    public static async Task<bool> InstallForCurrentOsAsync(string cache, CancellationToken ct)
    {
        if (OperatingSystem.IsWindows())
            return await InstallWindowsAsync(cache, ct);
        return await InstallFromSourceAsync(cache, ct);
    }

    private static async Task<bool> InstallWindowsAsync(string cache, CancellationToken ct)
    {
        var zipPath = Path.Join(cache, "aria2-win.zip");
        await DownloadAsync(Aria2Manifest.WindowsZipUrl, zipPath, ct);

        var extractDir = Path.Join(cache, "extract");
        Directory.CreateDirectory(extractDir);
        await ZipFile.ExtractToDirectoryAsync(zipPath, extractDir, overwriteFiles: true, cancellationToken: ct);

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
        var tarball = Path.Join(cache, "aria2-src.tar.gz");
        try
        {
            await DownloadAsync(Aria2Manifest.SourceTarballGzUrl, tarball, ct);
        }
        catch (HttpRequestException)
        {
            tarball = Path.Join(cache, "aria2-src.tar.xz");
            await DownloadAsync(Aria2Manifest.SourceTarballUrl, tarball, ct);
        }

        var extractDir = Path.Join(cache, "src");
        Directory.CreateDirectory(extractDir);
        var tar = await ProcessRunner.RunAsync(
            "tar",
            $"-xf \"{tarball}\" -C \"{extractDir}\" --strip-components=1",
            ct,
            workingDirectory: cache);
        if (tar.ExitCode != 0)
        {
            tar = await ProcessRunner.RunAsync("tar", $"-xf \"{tarball}\" -C \"{extractDir}\"", ct);
            if (tar.ExitCode != 0)
                return false;
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
            workingDirectory: extractDir);
        if (configure.ExitCode != 0)
        {
            configure = await ProcessRunner.RunAsync(
                "/bin/sh",
                $"./configure --prefix=\"{prefix}\"",
                ct,
                workingDirectory: extractDir);
            if (configure.ExitCode != 0)
                return false;
        }

        var jobs = Math.Max(1, Environment.ProcessorCount);
        var make = await ProcessRunner.RunAsync("make", $"-j{jobs}", ct, workingDirectory: extractDir);
        if (make.ExitCode != 0)
            return false;

        var install = await ProcessRunner.RunAsync("make", "install", ct, workingDirectory: extractDir);
        if (install.ExitCode != 0)
            return false;

        var built = Path.Join(prefix, "bin", "aria2c");
        if (!File.Exists(built))
        {
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
            _ = await ProcessRunner.RunAsync("chmod", $"+x \"{dest}\"", ct);
        return File.Exists(dest);
    }

    private static async Task DownloadAsync(string url, string dest, CancellationToken ct)
    {
        using var resp = await Http.GetAsync(new Uri(url), HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();
        var stream = await resp.Content.ReadAsStreamAsync(ct);
        await using (stream)
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
            await using (file)
                await stream.CopyToAsync(file, ct);
        }
    }

    private static HttpClient CreateHttp()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        c.DefaultRequestHeaders.UserAgent.ParseAdd(AppDefaults.UserAgent);
        return c;
    }
}
