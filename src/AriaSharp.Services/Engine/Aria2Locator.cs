
namespace AriaSharp.Services;

public sealed class Aria2Locator : IAria2Locator
{
    private readonly string? _explicitPath;

    public Aria2Locator(string? explicitPath = null) => _explicitPath = explicitPath;

    public Task<string> LocateAsync(CancellationToken ct)
    {
        _ = ct;

        if (!string.IsNullOrEmpty(_explicitPath))
        {
            var explicitFull = Path.IsPathRooted(_explicitPath)
                ? _explicitPath
                : Path.GetFullPath(_explicitPath);
            if (File.Exists(explicitFull))
                return Task.FromResult(explicitFull);
            throw new FileNotFoundException("aria2 not found at the explicit path.", explicitFull);
        }

        // 1. User-local install performed by AriaSharp (no admin)
        if (File.Exists(AppPaths.BundledAria2cPath))
            return Task.FromResult(AppPaths.BundledAria2cPath);

        // 2. PATH
        foreach (var name in new[] { "aria2c", "aria2" })
        {
            var found = FindInPath(name);
            if (!string.IsNullOrEmpty(found))
                return Task.FromResult(found);
        }

        // 3. Known system absolute paths
        foreach (var absolute in new[]
                 {
                     "/usr/bin/aria2c",
                     "/usr/local/bin/aria2c",
                     "/opt/homebrew/bin/aria2c",
                     "/usr/local/aria2/bin/aria2c",
                 })
        {
            if (File.Exists(absolute))
                return Task.FromResult(absolute);
        }

        throw new FileNotFoundException(
            "aria2c was not found in PATH or known locations. Use --aria2-path or -y / --install-deps.");
    }

    private static string? FindInPath(string fileName)
    {
        var pathVar = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathVar)) return null;

        foreach (var dir in pathVar.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Join(dir, fileName);
            if (File.Exists(candidate))
                return candidate;
            if (OperatingSystem.IsWindows())
            {
                var exe = Path.Join(dir, fileName + ".exe");
                if (File.Exists(exe))
                    return exe;
            }
        }
        return null;
    }
}
