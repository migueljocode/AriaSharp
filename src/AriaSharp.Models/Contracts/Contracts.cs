using System.Threading.Channels;

namespace AriaSharp.Models;

public interface IDownloadEngine : IAsyncDisposable
{
    Task StartAsync(EngineSettings settings, CancellationToken ct);
    Task<EngineSnapshot> GetSnapshotAsync(CancellationToken ct);
    Task SaveSessionAsync(CancellationToken ct);
    Task StopAsync(CancellationToken ct);
    Task<string> AddUriAsync(string[] uris, CancellationToken ct);
    ChannelReader<EngineEvent> Events { get; }
}

public interface IDownloadSession
{
    SessionState State { get; }
    TimeSpan Elapsed { get; }
    Task<RunOutcome> RunAsync(CancellationToken ct);
    Task RequestStopAsync();
}

public interface IAria2Locator { Task<string> LocateAsync(CancellationToken ct); }

public interface IPrerequisiteInstaller
{
    Task<bool> IsInstalledAsync(string package, CancellationToken ct);
}

public interface ISettingsStore
{
    Task<AppSettings> LoadAsync(CancellationToken ct);
    Task SaveAsync(AppSettings settings, CancellationToken ct);
}
