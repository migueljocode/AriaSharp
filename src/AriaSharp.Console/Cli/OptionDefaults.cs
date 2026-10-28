namespace AriaSharp.App;

/// <summary>Default values shown in help. One place, so the full-screen help and the plain help can never disagree.</summary>
internal static class OptionDefaults
{
    public static string? For(string longName) => longName switch
    {
        "jobs" => $"{AppDefaults.MaxConcurrentDownloads}",
        "connections" or "split" or "per-server" => $"{AppDefaults.MaxConnectionPerServer}",
        "min-split-size" => $"{AppDefaults.MinSplitSize}",
        "refresh" => $"{AppDefaults.RefreshIntervalMs}ms",
        "output" => $"{AppDefaults.OutputDirDefault}",
        "session" => $"{AppDefaults.SessionFileName}",
        _ => null,
    };
}
