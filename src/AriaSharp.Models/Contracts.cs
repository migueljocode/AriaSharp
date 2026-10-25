using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;

namespace AriaSharp.Models;

public interface IDownloadEngine : IAsyncDisposable
{
    Task StartAsync(EngineSettings settings, CancellationToken ct);
    Task<EngineSnapshot> GetSnapshotAsync(CancellationToken ct);
    Task SaveSessionAsync(CancellationToken ct);
    Task ShutdownAsync(CancellationToken ct);
    Task<long> AddUriAsync(string[] uris, CancellationToken ct);
    ChannelReader<EngineEvent> Events { get; }
}

public sealed class EngineSettings
{
    public required string Aria2Path { get; init; }
    public required string OutputDir { get; init; }
    public required string SessionFile { get; init; }
    public required string TempInputFile { get; init; }
    public required string TempConfFile { get; init; }
    public required string Secret { get; init; }
    public required int Port { get; init; }
    public required string[] Args { get; init; }
}

public interface IDownloadSession
{
    SessionState State { get; }
    ChannelReader<SessionUpdate> Updates { get; }
    Task<RunOutcome> RunAsync(CancellationToken ct);
    Task RequestStopAsync();
}

public interface IAria2Locator { Task<string> LocateAsync(CancellationToken ct); }
public interface IPrerequisiteInstaller
{
    Task<bool> InstallAsync(string package, CancellationToken ct);
    Task<bool> IsInstalledAsync(string package, CancellationToken ct);
}
public interface ISettingsStore
{
    Task<AppSettings> LoadAsync(CancellationToken ct);
    Task SaveAsync(AppSettings settings, CancellationToken ct);
}