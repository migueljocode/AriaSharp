using System.Text.Json;
using System.Text.Json.Serialization;

namespace AriaSharp.Models;

public sealed class AppDefaults
{
    public const int MaxConcurrentDownloads = 1;
    public const int Split = 16;
    public const int MaxConnectionPerServer = 16;
    public const bool AutoFileRenaming = false;
    public const bool AllowOverwrite = false;
    public const bool SaveSession = true;
    public const int SaveSessionInterval = 60;
    public const int RefreshIntervalMs = 500;
    public const int MinRefreshIntervalMs = 100;
    public const int MaxRefreshIntervalMs = 5000;
    public const int PollFloorMs = 250;
    public const int RpcReadyTimeoutMs = 10000;
    public const int GracefulStopTimeoutMs = 3000;
    public const int IdlePollsRequired = 2;
    public const int MinTerminalWidth = 76;
    public const int FullScreenTerminalWidth = 100;
    public const int MinTerminalHeight = 14;
    public const int FullScreenTerminalHeight = 20;
    public const int LogoFullWidth = 80;
    public const int LogoStackedWidth = 90;
    public const int LogoTextWidth = 52;
    public const string SessionFileName = "resume.session";
    public const string OutputDirDefault = ".";
    public const string Aria2Version = "1.37.0";
    public const string Aria2ReleaseTag = "aria2-1.37.0";
    public const string ConfigFileName = "ariasharp.json";
    public const string EnvPrefix = "ARIASHARP_";
    public const string UserAgent = "AriaSharp/1.0.0";
    public const double EmaAlpha = 0.3;
    public const int EtaStableTicks = 3;
    public const string LogFileName = "ariasharp.log";
}

public sealed class AppSettings
{
    public int MaxConcurrentDownloads { get; set; } = AppDefaults.MaxConcurrentDownloads;
    public int Split { get; set; } = AppDefaults.Split;
    public int MaxConnectionPerServer { get; set; } = AppDefaults.MaxConnectionPerServer;
    public bool AutoFileRenaming { get; set; } = AppDefaults.AutoFileRenaming;
    public bool AllowOverwrite { get; set; } = AppDefaults.AllowOverwrite;
    public bool SaveSession { get; set; } = AppDefaults.SaveSession;
    public int SaveSessionInterval { get; set; } = AppDefaults.SaveSessionInterval;
    public int RefreshIntervalMs { get; set; } = AppDefaults.RefreshIntervalMs;
    public int Jobs { get; set; } = AppDefaults.MaxConcurrentDownloads;
    public int Connections { get; set; } = AppDefaults.MaxConnectionPerServer;
    public bool Inline { get; set; }
    public bool Plain { get; set; }
    public bool NoColor { get; set; }
    public bool NoPager { get; set; }
    public bool NoInstall { get; set; }
    public bool Verbose { get; set; }
    public bool Yes { get; set; }
    public bool Force { get; set; }
    public bool KeepListFiles { get; set; }
    public bool Resume { get; set; }
    public bool ProbeSizes { get; set; }
    public bool InstallDeps { get; set; }
    public bool InstallAria2 { get; set; }
    public bool Help { get; set; }
    public bool Version { get; set; }
    public string? OutputDir { get; set; }
    public string? SessionFile { get; set; }
    public string? ConfigFile { get; set; }
    public string? Aria2Path { get; set; }
    public string? SpeedLimit { get; set; }
    public string? Limit { get; set; }
    public IReadOnlyList<string> Inputs { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> ExtraOptions { get; set; } = Array.Empty<string>();
}

[JsonSerializable(typeof(AppSettings))]
public partial class AppSettingsJsonContext : JsonSerializerContext
{
}

public sealed class UiSettings
{
    public bool Color { get; init; } = true;
    public bool Unicode { get; init; } = true;
    public bool AlternateScreen { get; init; } = true;
    public bool ShowConnections { get; init; } = true;
    public bool ShowEta { get; init; } = true;
    public int TerminalWidth { get; init; } = 100;
    public int TerminalHeight { get; init; } = 30;
}