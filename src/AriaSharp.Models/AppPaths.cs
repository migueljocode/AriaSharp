using System.IO;

namespace AriaSharp.Models;

public static class AppPaths
{
    public static string ConfigDirectory { get; }
    public static string SettingsFile { get; }

    static AppPaths()
    {
        var baseDir = OperatingSystem.IsWindows()
            ? Environment.GetEnvironmentVariable("APPDATA") ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            : Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") ?? Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
        ConfigDirectory = Path.Join(baseDir, "ariasharp");
        SettingsFile = Path.Join(ConfigDirectory, "settings.json");
    }
}
