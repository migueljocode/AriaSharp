
namespace AriaSharp.Services;

/// <summary>
/// Reads environment variables into a <see cref="SettingsOverlay"/> so they form the environment
/// layer in the precedence chain (defaults &lt; config file &lt; environment &lt; CLI). Honoured
/// here: NO_COLOR, ARIASHARP_ASCII, ARIASHARP_CONFIG, and the generic ARIASHARP_&lt;KEY&gt;
/// pattern for the settings that have a config key.
/// </summary>
public static class EnvOverlay
{
    public static SettingsOverlay FromEnvironment()
    {
        var overlay = new SettingsOverlay();

        if (IsTrue("NO_COLOR") || IsTrue("ARIASHARP_NO_COLOR"))
            overlay.NoColor = true;

        if (IsSet("ARIASHARP_ASCII"))
            overlay.Ascii = true;

        var config = Environment.GetEnvironmentVariable("ARIASHARP_CONFIG");
        if (!string.IsNullOrWhiteSpace(config))
            overlay.ConfigFile = config;

        var network = Environment.GetEnvironmentVariable("ARIASHARP_NETWORK");
        if (!string.IsNullOrWhiteSpace(network))
        {
            // online/offline is consumed by the engine layer; expose as a marker via proxy env.
            // Not persisted to AppSettings today (M2).
        }

        ApplyInt("ARIASHARP_JOBS", v => overlay.MaxConcurrentDownloads = v);
        ApplyInt("ARIASHARP_CONNECTIONS", v => { overlay.Connections = v; overlay.Split = v; overlay.MaxConnectionPerServer = v; });
        ApplyInt("ARIASHARP_SPLIT", v => overlay.Split = v);
        ApplyInt("ARIASHARP_MAX_CONNECTION_PER_SERVER", v => overlay.MaxConnectionPerServer = v);
        ApplyInt("ARIASHARP_REFRESH", v => overlay.RefreshIntervalMs = v);
        ApplyInt("ARIASHARP_POLL", v => overlay.PollIntervalMs = v);

        ApplyString("ARIASHARP_OUTPUT", v => overlay.OutputDir = v);
        ApplyString("ARIASHARP_SESSION", v => overlay.SessionFile = v);
        ApplyString("ARIASHARP_LIMIT", v => overlay.SpeedLimit = v);
        ApplyString("ARIASHARP_ARIA2_PATH", v => overlay.Aria2Path = v);
        ApplyString("ARIASHARP_MIN_SPLIT_SIZE", v => overlay.ExtraOptions = (overlay.ExtraOptions ?? Array.Empty<string>()).Append($"min-split-size={v}").ToArray());

        ApplyFlag("ARIASHARP_INLINE", () => overlay.Inline = true);
        ApplyFlag("ARIASHARP_PLAIN", () => overlay.Plain = true);
        ApplyFlag("ARIASHARP_NO_PAGER", () => overlay.NoPager = true);
        ApplyFlag("ARIASHARP_FORCE", () => overlay.Force = true);
        ApplyFlag("ARIASHARP_RESUME", () => overlay.Resume = true);
        ApplyFlag("ARIASHARP_VERBOSE", () => overlay.Verbose = true);
        ApplyFlag("ARIASHARP_YES", () => overlay.Yes = true);
        ApplyFlag("ARIASHARP_KEEP_LIST", () => overlay.KeepListFiles = true);
        ApplyFlag("ARIASHARP_NO_INSTALL", () => overlay.NoInstall = true);

        return overlay;
    }

    private static bool IsSet(string name) => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(name));

    private static bool IsTrue(string name)
    {
        var v = Environment.GetEnvironmentVariable(name);
        return string.Equals(v, "1", StringComparison.Ordinal)
            || string.Equals(v, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(v, "on", StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplyInt(string name, Action<int> set)
    {
        var v = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(v)) return;
        if (int.TryParse(v, out var n)) set(n);
    }

    private static void ApplyString(string name, Action<string> set)
    {
        var v = Environment.GetEnvironmentVariable(name);
        if (!string.IsNullOrWhiteSpace(v)) set(v);
    }

    private static void ApplyFlag(string name, Action set)
    {
        if (IsTrue(name)) set();
    }
}
