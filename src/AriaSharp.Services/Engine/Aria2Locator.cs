namespace AriaSharp.Services;

public sealed class Aria2Locator(string? explicitPath = null) : IAria2Locator
{
    public Task<string> LocateAsync(CancellationToken ct)
    {
        _ = ct;

        if (explicitPath is { Length: > 0 })
        {
            var full = Path.IsPathRooted(explicitPath)
                ? explicitPath
                : Path.GetFullPath(explicitPath);
            if (File.Exists(full))
                return Task.FromResult(full);
            throw new FileNotFoundException("aria2 not found at the explicit path.", full);
        }

        if (File.Exists(AppPaths.BundledAria2cPath))
            return Task.FromResult(AppPaths.BundledAria2cPath);

        foreach (var name in (string[])["aria2c", "aria2"])
        {
            if (FindInPath(name) is { } found)
                return Task.FromResult(found);
        }

        foreach (var absolute in (string[])
                 [
                     "/usr/bin/aria2c",
                     "/usr/local/bin/aria2c",
                     "/opt/homebrew/bin/aria2c",
                     "/usr/local/aria2/bin/aria2c",
                 ])
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
        if (pathVar is not { Length: > 0 })
            return null;

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
