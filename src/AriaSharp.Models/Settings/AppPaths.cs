namespace AriaSharp.Models;

/// <summary>
/// User-writable locations for config and bundled tools (no admin required).
/// </summary>
public static class AppPaths
{
    public static string ConfigDirectory { get; }
    public static string SettingsFile { get; }

    /// <summary>Per-user tools root, e.g. ~/.local/share/ariasharp or %LOCALAPPDATA%\AriaSharp.</summary>
    public static string DataDirectory { get; }

    /// <summary>Directory containing aria2c binary installed by AriaSharp.</summary>
    public static string ToolsBinDirectory { get; }

    /// <summary>Scratch area for downloads/builds; deleted after install.</summary>
    public static string ToolsCacheDirectory { get; }

    public static string BundledAria2cPath { get; }

    static AppPaths()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (OperatingSystem.IsWindows())
        {
            var local = Environment.GetEnvironmentVariable("LOCALAPPDATA")
                ?? Path.Join(home, "AppData", "Local");
            DataDirectory = Path.Join(local, "AriaSharp");
            var appData = Environment.GetEnvironmentVariable("APPDATA") ?? Path.Join(home, "AppData", "Roaming");
            ConfigDirectory = Path.Join(appData, "ariasharp");
        }
        else
        {
            var xdgData = Environment.GetEnvironmentVariable("XDG_DATA_HOME")
                ?? Path.Join(home, ".local", "share");
            DataDirectory = Path.Join(xdgData, "ariasharp");
            var xdgConfig = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME")
                ?? Path.Join(home, ".config");
            ConfigDirectory = Path.Join(xdgConfig, "ariasharp");
        }

        SettingsFile = Path.Join(ConfigDirectory, "settings.json");
        ToolsBinDirectory = Path.Join(DataDirectory, "bin");
        ToolsCacheDirectory = Path.Join(DataDirectory, "cache");
        BundledAria2cPath = Path.Join(
            ToolsBinDirectory,
            OperatingSystem.IsWindows() ? "aria2c.exe" : "aria2c");
    }

    public static void EnsureDataDirectories()
    {
        Directory.CreateDirectory(ConfigDirectory);
        Directory.CreateDirectory(ToolsBinDirectory);
        Directory.CreateDirectory(ToolsCacheDirectory);
    }
}
