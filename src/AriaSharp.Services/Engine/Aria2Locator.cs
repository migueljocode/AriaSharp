using System.IO;
using System.Threading.Tasks;

using AriaSharp.Models;

namespace AriaSharp.Services;

public sealed class Aria2Locator : IAria2Locator
{
    private readonly string? _explicitPath;

    public Aria2Locator(string? explicitPath = null)
    {
        _explicitPath = explicitPath;
    }

    public Task<string> LocateAsync(CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_explicitPath))
        {
            if (File.Exists(_explicitPath))
            {
                return Task.FromResult(_explicitPath!);
            }
            throw new FileNotFoundException("aria2 not found at the explicit path.", _explicitPath);
        }
        var candidates = new[]
        {
            "aria2c", "aria2",
            "/usr/bin/aria2c", "/usr/local/bin/aria2c",
            "/opt/homebrew/bin/aria2c", "/usr/local/aria2/bin/aria2c",
        };
        foreach (var c in candidates)
        {
            var path = FindInPath(c);
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                return Task.FromResult(path!);
            }
        }
        throw new FileNotFoundException("aria2c was not found in PATH or known locations. Use --aria2-path or run the installer.");
    }

    private static string? FindInPath(string command)
    {
        var pathVar = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var extensions = Environment.OSVersion.Platform == PlatformID.Win32NT
            ? new[] { ".exe", ".cmd", ".bat" }
            : new[] { string.Empty };
        foreach (var dir in pathVar.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var ext in extensions)
            {
                var full = Path.Combine(dir, command + ext);
                if (File.Exists(full))
                {
                    return full;
                }
            }
        }
        return null;
    }
}
