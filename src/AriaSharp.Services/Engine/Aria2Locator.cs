using AriaSharp.Models;

namespace AriaSharp.Services;

public sealed class Aria2Locator : IAria2Locator
{
    private readonly string? _explicitPath;

    public Aria2Locator(string? explicitPath = null) => _explicitPath = explicitPath;

    public Task<string> LocateAsync(CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_explicitPath))
        {
            if (File.Exists(_explicitPath))
                return Task.FromResult(_explicitPath);
            throw new FileNotFoundException("aria2 not found at the explicit path.", _explicitPath);
        }

        // Bare names: search PATH
        foreach (var name in new[] { "aria2c", "aria2" })
        {
            var found = FindInPath(name);
            if (!string.IsNullOrEmpty(found))
                return Task.FromResult(found);
        }

        // Absolute fallbacks (do not Path.Join these with PATH dirs)
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

    private static string? FindInPath(string command)
    {
        var pathVar = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var extensions = OperatingSystem.IsWindows()
            ? new[] { ".exe", ".cmd", ".bat", string.Empty }
            : new[] { string.Empty };

        foreach (var dir in pathVar.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var ext in extensions)
            {
                var full = Path.Join(dir, command + ext);
                if (File.Exists(full))
                    return full;
            }
        }
        return null;
    }
}
