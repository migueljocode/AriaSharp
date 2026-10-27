using System.IO.Compression;
using System.Security.Cryptography;

namespace AriaSharp.Services;

/// <summary>Download official aria2 artifacts with SHA-256 pin; TLS-capable source build on Unix.</summary>
internal static class Aria2GitHubInstaller
{
    private static readonly HttpClient Http = CreateHttp();

    public static async Task<(bool Ok, string Message, string? LogPath)> InstallForCurrentOsAsync(
        string cache,
        CancellationToken ct)
    {
        var logPath = Path.Join(cache, "install.log");
        await using var log = new StreamWriter(new FileStream(
            logPath,
            new FileStreamOptions
            {
                Mode = FileMode.Create,
                Access = FileAccess.Write,
                Share = FileShare.Read,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
            }))
        { AutoFlush = true };

        try
        {
            if (OperatingSystem.IsWindows())
            {
                var ok = await InstallWindowsAsync(cache, log, ct);
                return (ok, ok ? "Windows zip installed" : "Windows zip install failed", logPath);
            }

            var (built, msg) = await InstallFromSourceAsync(cache, log, ct);
            return (built, msg, logPath);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            await log.WriteLineAsync($"error: {ex.Message}");
            return (false, ex.Message, logPath);
        }
    }

    private static async Task<bool> InstallWindowsAsync(string cache, StreamWriter log, CancellationToken ct)
    {
        var zipPath = Path.Join(cache, "aria2-win.zip");
        await log.WriteLineAsync($"download {Aria2Manifest.WindowsZipUrl}");
        await DownloadAsync(Aria2Manifest.WindowsZipUrl, zipPath, ct);
        await VerifySha256Async(zipPath, Aria2Manifest.WindowsZipSha256, log, ct);

        var extractDir = Path.Join(cache, "extract");
        Directory.CreateDirectory(extractDir);
        await ZipFile.ExtractToDirectoryAsync(zipPath, extractDir, overwriteFiles: true, cancellationToken: ct);

        var exe = Directory.EnumerateFiles(extractDir, "aria2c.exe", SearchOption.AllDirectories).FirstOrDefault();
        if (exe is null)
        {
            await log.WriteLineAsync("aria2c.exe not found in zip");
            return false;
        }

        var dest = AppPaths.BundledAria2cPath;
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        File.Copy(exe, dest, overwrite: true);
        await log.WriteLineAsync($"installed {dest}");
        return File.Exists(dest);
    }

    private static async Task<(bool Ok, string Message)> InstallFromSourceAsync(
        string cache,
        StreamWriter log,
        CancellationToken ct)
    {
        var missing = await PreflightBuildToolsAsync(log, ct);
        if (missing is { Length: > 0 })
            return (false, $"missing build tools: {missing}");

        var tarball = Path.Join(cache, "aria2-src.tar.gz");
        await log.WriteLineAsync($"download {Aria2Manifest.SourceTarballGzUrl}");
        await DownloadAsync(Aria2Manifest.SourceTarballGzUrl, tarball, ct);
        await VerifySha256Async(tarball, Aria2Manifest.SourceTarballGzSha256, log, ct);

        var extractDir = Path.Join(cache, "src");
        Directory.CreateDirectory(extractDir);
        var tar = await ProcessRunner.RunAsync(
            "tar",
            $"-xf \"{tarball}\" -C \"{extractDir}\" --strip-components=1",
            ct,
            workingDirectory: cache);
        await log.WriteLineAsync($"tar exit {tar.ExitCode}");
        if (tar.ExitCode != 0)
            return (false, "tar extract failed");

        var prefix = Path.Join(cache, "prefix");
        Directory.CreateDirectory(prefix);

        var tlsFlag = OperatingSystem.IsMacOS() ? "--with-appletls" : "--with-openssl";
        var configureCmd =
            $"./configure --prefix=\"{prefix}\" {tlsFlag} --without-libxml2 --without-libexpat --disable-nls";
        await log.WriteLineAsync(configureCmd);
        var configure = await ProcessRunner.RunAsync("/bin/sh", configureCmd, ct, workingDirectory: extractDir);
        await log.WriteLineAsync($"configure exit {configure.ExitCode}");
        if (configure.ExitCode != 0)
            return (false, "configure failed (TLS required; see install log)");

        var jobs = Math.Max(1, Environment.ProcessorCount);
        var make = await ProcessRunner.RunAsync("make", $"-j{jobs}", ct, workingDirectory: extractDir);
        await log.WriteLineAsync($"make exit {make.ExitCode}");
        if (make.ExitCode != 0)
            return (false, "make failed");

        var install = await ProcessRunner.RunAsync("make", "install", ct, workingDirectory: extractDir);
        await log.WriteLineAsync($"make install exit {install.ExitCode}");
        if (install.ExitCode != 0)
            return (false, "make install failed");

        var built = Path.Join(prefix, "bin", "aria2c");
        if (!File.Exists(built))
            return (false, "aria2c binary missing after install");

        var dest = AppPaths.BundledAria2cPath;
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        File.Copy(built, dest, overwrite: true);
        _ = await ProcessRunner.RunAsync("chmod", $"+x \"{dest}\"", ct);

        var ver = await ProcessRunner.RunAsync(dest, "--version", ct);
        await log.WriteLineAsync(ver.StdOut);
        if (ver.ExitCode != 0 || !ver.StdOut.Contains("HTTPS", StringComparison.OrdinalIgnoreCase))
            return (false, "aria2c --version missing HTTPS support");

        await log.WriteLineAsync($"installed {dest}");
        return (true, "source build installed with HTTPS");
    }

    private static async Task<string?> PreflightBuildToolsAsync(StreamWriter log, CancellationToken ct)
    {
        var missing = new List<string>();
        foreach (var tool in new[] { "cc", "make", "tar" })
        {
            var r = await ProcessRunner.RunAsync("which", tool, ct);
            if (r.ExitCode != 0)
                missing.Add(tool);
        }
        if (missing.Count > 0)
        {
            var hint = OperatingSystem.IsMacOS()
                ? "install Xcode CLT: xcode-select --install"
                : "install build-essential + libssl-dev + zlib1g-dev (or distro equivalent)";
            await log.WriteLineAsync($"preflight missing: {string.Join(", ", missing)}; {hint}");
            return $"{string.Join(", ", missing)} ({hint})";
        }
        return null;
    }

    private static async Task VerifySha256Async(string path, string expectedHex, StreamWriter log, CancellationToken ct)
    {
        await using var stream = new FileStream(
            path,
            new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.Read,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
            });
        var hash = await SHA256.HashDataAsync(stream, ct);
        var actual = Convert.ToHexString(hash).ToUpperInvariant();
        var expected = expectedHex.ToUpperInvariant();
        await log.WriteLineAsync($"sha256 {actual}");
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
            throw new InvalidOperationException($"SHA-256 mismatch for {Path.GetFileName(path)} (expected {expected})");
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
