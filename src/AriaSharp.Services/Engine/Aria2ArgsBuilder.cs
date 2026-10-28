namespace AriaSharp.Services;

public interface IAria2ArgsBuilder
{
    string[] Build(EngineSettings settings, AppSettings appSettings);
}

public sealed class Aria2ArgsBuilder : IAria2ArgsBuilder
{
    private static readonly string[] BlockedKeys =
    [
        "rpc-secret", "rpc-listen-port", "rpc-listen-all", "rpc-user", "rpc-pass",
        "enable-rpc", "conf-path", "no-conf", "stop-with-process", "input-file",
        "save-session", "save-session-interval",
    ];

    public string[] Build(EngineSettings settings, AppSettings appSettings)
    {
        ThrowIfNull(settings);
        ThrowIfNull(appSettings);

        List<string> args =
        [
            "--enable-rpc",
            $"--rpc-listen-port={settings.Port}",
            "--rpc-listen-all=false",
            $"--conf-path={settings.ConfFile}",
            $"--stop-with-process={Environment.ProcessId}",
            $"--input-file={settings.InputFile}",
            $"--dir={settings.OutputDir}",
            $"--max-concurrent-downloads={appSettings.MaxConcurrentDownloads}",
            $"--split={appSettings.Split}",
            $"--max-connection-per-server={appSettings.MaxConnectionPerServer}",
            $"--auto-file-renaming={ToAriaBool(appSettings.AutoFileRenaming)}",
            $"--allow-overwrite={ToAriaBool(appSettings.AllowOverwrite)}",
            "--summary-interval=0",
            "--show-console-readout=false",
            "--console-log-level=warn",
            "--enable-color=false",
            "--download-result=hide",
        ];

        if (appSettings.SaveSession)
        {
            args.Add($"--save-session={settings.SessionFile}");
            args.Add($"--save-session-interval={appSettings.SaveSessionInterval}");
        }

        // When resuming (-s), also feed the session file as an extra input source.
        if (appSettings.Resume && !string.IsNullOrEmpty(settings.SessionFile))
            args.Add($"--input-file={settings.SessionFile}");

        if (appSettings.SpeedLimit is { Length: > 0 })
            args.Add($"--max-overall-download-limit={appSettings.SpeedLimit}");

        // --min-split-size before ExtraOptions so a user override still wins.
        args.Add($"--min-split-size={AppDefaults.MinSplitSize}");

        foreach (var pair in appSettings.ExtraOptions)
        {
            if (pair is not { Length: > 0 })
                continue;
            if (!TryParseOpt(pair, out var key, out var value))
                continue;
            if (IsBlocked(key))
                continue;
            args.Add($"--{key}={value}");
        }

        return [.. args];
    }

    private static string ToAriaBool(bool value) => value ? "true" : "false";

    private static bool TryParseOpt(string pair, out string key, out string value)
    {
        key = string.Empty;
        value = string.Empty;
        var idx = pair.IndexOf('=', StringComparison.Ordinal);
        if (idx <= 0 || idx == pair.Length - 1)
            return false;
        key = pair[..idx].Trim();
        value = pair[(idx + 1)..].Trim();
        return key.Length > 0;
    }

    private static bool IsBlocked(string key) =>
        BlockedKeys.Contains(key, StringComparer.OrdinalIgnoreCase)
        || key.StartsWith("rpc-", StringComparison.OrdinalIgnoreCase)
        || key.StartsWith("on-", StringComparison.OrdinalIgnoreCase);
}
