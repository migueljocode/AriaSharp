using System.Threading.Channels;

using AriaSharp.Models;

namespace AriaSharp.Services;

public interface IAria2Engine : IDownloadEngine
{
    Task<string> GetVersionAsync(CancellationToken ct);
}

public sealed class Aria2Engine : IAria2Engine
{
    private readonly HttpClient _httpClient;
    private readonly Channel<EngineEvent> _channel;
    private string _version = string.Empty;
    private bool _disposed;
    public Aria2Engine() { _httpClient = new HttpClient(); _channel = Channel.CreateUnbounded<EngineEvent>(new UnboundedChannelOptions { SingleReader = true }); }
    public ChannelReader<EngineEvent> Events => _channel.Reader;
    public Task StartAsync(EngineSettings settings, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _httpClient.BaseAddress = new Uri($"http://127.0.0.1:{settings.Port}/");
        return Task.CompletedTask;
    }
    public Task<EngineSnapshot> GetSnapshotAsync(CancellationToken ct)
    {
        var empty = Array.Empty<DownloadItem>();
        return Task.FromResult(new EngineSnapshot
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Active = empty, Waiting = empty, Stopped = empty, Paused = empty,
            Complete = empty, Failed = empty, Skipped = empty, Interrupted = empty,
        });
    }
    public Task SaveSessionAsync(CancellationToken ct) => Task.CompletedTask;
    public Task ShutdownAsync(CancellationToken ct) => Task.CompletedTask;
    public Task<long> AddUriAsync(string[] uris, CancellationToken ct) => Task.FromResult(0L);
    public Task<string> GetVersionAsync(CancellationToken ct) { if (string.IsNullOrEmpty(_version)) _version = "1.37.0"; return Task.FromResult(_version); }
    public ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;
        _disposed = true;
        _channel.Writer.TryComplete();
        _httpClient.Dispose();
        return ValueTask.CompletedTask;
    }
}