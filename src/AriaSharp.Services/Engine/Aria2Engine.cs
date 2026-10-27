using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;


namespace AriaSharp.Services;

public interface IAria2Engine : IDownloadEngine
{
    Task<string> GetVersionAsync(CancellationToken ct);
}

/// <summary>
/// Manages one aria2c process and talks to it over loopback JSON-RPC.
/// Secret lives only in an owner-only conf file — never on the command line.
/// </summary>
public sealed class Aria2Engine : IAria2Engine
{
    private readonly Channel<EngineEvent> _channel =
        Channel.CreateUnbounded<EngineEvent>(new UnboundedChannelOptions { SingleReader = true });

    private Aria2Process? _process;
    private ChannelWriter<string>? _logWriter;
    private Task _logTask = Task.CompletedTask;
    private Aria2RpcClient? _rpc;
    private Aria2NotificationListener? _notifications;
    private string _version = string.Empty;
    private bool _disposed;
    private SessionSaveCoordinator? _sessionSave;

    public ChannelReader<EngineEvent> Events => _channel.Reader;

    public async Task StartAsync(EngineSettings settings, CancellationToken ct)
    {
        ThrowIfNull(settings);
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_process is not null)
            throw new InvalidOperationException("Engine already started.");

        await Aria2ConfWriter.WriteAsync(settings.ConfFile, settings.Secret, settings.Port, ct);

        var logChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });
        _logWriter = logChannel.Writer;
        _logTask = DrainLogsAsync(logChannel.Reader, settings.LogFile);

        _process = new Aria2Process(settings.Aria2Path, settings.Args.ToArray(), logChannel.Writer);

        _rpc = new Aria2RpcClient(CreateLoopbackClient(settings.Port), settings.Secret);

        await WaitForRpcAsync(ct);
        try { File.Delete(settings.ConfFile); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }

        _sessionSave = new SessionSaveCoordinator(SaveSessionAsync);
        _notifications = new Aria2NotificationListener(settings.Port, _channel.Writer, OnNotification);
        try
        {
            await _notifications.StartAsync(ct);
        }
        catch (Exception ex) when (ex is System.Net.WebSockets.WebSocketException or HttpRequestException or IOException or TimeoutException)
        {
            // Notifications are best-effort; HTTP polling still works.
            await _notifications.DisposeAsync();
            _notifications = null;
        }
    }

    public async Task<EngineSnapshot> GetSnapshotAsync(CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var rpc = _rpc ?? throw new InvalidOperationException("Engine not started.");

        var keys = new[]
        {
            "gid", "status", "totalLength", "completedLength", "downloadSpeed",
            "uploadSpeed", "connections", "dir", "files", "errorCode", "errorMessage",
        };

        var multicall = await rpc.MulticallAsync(
        [
            ("aria2.tellActive", [keys]),
            ("aria2.tellWaiting", [0, 1000, keys]),
            ("aria2.tellStopped", [0, 1000, keys]),
            ("aria2.getGlobalStat", null),
        ], ct);

        var active = Aria2SnapshotMapper.MapItems(Aria2SnapshotMapper.ExtractMulticallResult(multicall, 0));
        var (waiting, paused) = SplitPaused(Aria2SnapshotMapper.MapItems(Aria2SnapshotMapper.ExtractMulticallResult(multicall, 1)));
        var stoppedRaw = Aria2SnapshotMapper.MapItems(Aria2SnapshotMapper.ExtractMulticallResult(multicall, 2));
        var global = Aria2SnapshotMapper.ExtractMulticallResult(multicall, 3);

        var complete = new List<DownloadItem>();
        var failed = new List<DownloadItem>();
        var skipped = new List<DownloadItem>();
        var interrupted = new List<DownloadItem>();
        var stopped = new List<DownloadItem>();

        foreach (var item in stoppedRaw)
        {
            switch (item.State)
            {
                case DownloadState.Complete:
                    complete.Add(item);
                    break;
                case DownloadState.Failed when IsSkippedCode(item.ErrorCode):
                    // 13 file already exists · 11 duplicate download → skipped, not failed
                    skipped.Add(item);
                    break;
                case DownloadState.Failed:
                    failed.Add(item);
                    break;
                case DownloadState.Interrupted:
                    interrupted.Add(item);
                    break;
                default:
                    stopped.Add(item);
                    break;
            }
        }

        long numActive = 0, numWaiting = 0, dlSpeed = 0, ulSpeed = 0;
        if (global.ValueKind == JsonValueKind.Object)
        {
            numActive = ReadGlobalLong(global, "numActive");
            numWaiting = ReadGlobalLong(global, "numWaiting");
            dlSpeed = ReadGlobalLong(global, "downloadSpeed");
            ulSpeed = ReadGlobalLong(global, "uploadSpeed");
        }

        return new EngineSnapshot
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Active = active,
            Waiting = waiting,
            Stopped = stopped,
            Paused = paused,
            Complete = complete,
            Failed = failed,
            Skipped = skipped,
            Interrupted = interrupted,
            NumActive = (int)numActive,
            NumWaiting = (int)numWaiting,
            TotalDownloadSpeed = dlSpeed,
            TotalUploadSpeed = ulSpeed,
            CompletedLength = active.Sum(i => i.CompletedLength)
                + complete.Sum(i => i.CompletedLength)
                + failed.Sum(i => i.CompletedLength),
            TotalLength = active.Sum(i => i.TotalLength)
                + waiting.Sum(i => i.TotalLength)
                + complete.Sum(i => i.TotalLength),
        };
    }

    private void OnNotification(EngineEvent ev)
    {
        if (!SessionSaveCoordinator.ShouldSaveSession(ev.Kind))
            return;
        _sessionSave?.RequestSave();
    }

    public async Task SaveSessionAsync(CancellationToken ct)
    {
        var rpc = _rpc ?? throw new InvalidOperationException("Engine not started.");
        await rpc.CallAsync("aria2.saveSession", null, ct);
    }

    /// <summary>End-of-run: coalesce with in-flight notification saves, then ensure one final save.</summary>
    public async Task FlushSessionAsync(CancellationToken ct)
    {
        if (_sessionSave is not null)
            await _sessionSave.FlushAsync(ct);
        else
            await SaveSessionAsync(ct);
    }

    public async Task ShutdownAsync(CancellationToken ct)
    {
        if (_rpc is null) return;
        try
        {
            await _rpc.CallAsync("aria2.shutdown", null, ct);
        }
        catch (HttpRequestException) { }
        catch (InvalidOperationException) { }
        catch (TaskCanceledException) { }
    }

    public async Task<string> AddUriAsync(string[] uris, CancellationToken ct)
    {
        ThrowIfNull(uris);
        var rpc = _rpc ?? throw new InvalidOperationException("Engine not started.");
        var gid = await rpc.CallAsync("aria2.addUri", [uris], ct);
        return gid.ValueKind == JsonValueKind.String ? gid.GetString()! : throw new InvalidOperationException("aria2.addUri returned no GID.");
    }

    public async Task<string> GetVersionAsync(CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_version)) return _version;
        var rpc = _rpc ?? throw new InvalidOperationException("Engine not started.");
        var result = await rpc.CallAsync("aria2.getVersion", null, ct);
        if (result.ValueKind == JsonValueKind.Object && result.TryGetProperty("version", out var v))
            _version = v.GetString() ?? AppDefaults.Aria2Version;
        else
            _version = AppDefaults.Aria2Version;
        return _version;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            if (_rpc is not null)
            {
                try { await ShutdownAsync(CancellationToken.None); }
                catch (HttpRequestException) { }
                catch (InvalidOperationException) { }
                catch (TaskCanceledException) { }
                catch (TimeoutException) { }
                catch (IOException) { }
            }
        }
        finally
        {
            if (_notifications is not null)
                await _notifications.DisposeAsync();
            if (_process is not null)
                await _process.DisposeAsync();
            if (_rpc is not null)
                await _rpc.DisposeAsync();
            _logWriter?.TryComplete();
            await _logTask;
            _channel.Writer.TryComplete();
        }
    }

    // Loopback RPC must never go through a proxy.
    private static readonly SocketsHttpHandler LoopbackHandler = new() { UseProxy = false };

    private static HttpClient CreateLoopbackClient(int port) =>
        new(LoopbackHandler, disposeHandler: false)
        {
            BaseAddress = new Uri($"http://127.0.0.1:{port}/"),
            Timeout = TimeSpan.FromSeconds(10),
        };

    public static int FindFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
            listener.Dispose();
        }
    }

    public static string CreateSecret()
    {
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes);
    }

    public async Task PauseAllAsync(CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var rpc = _rpc ?? throw new InvalidOperationException("Engine not started.");
        await rpc.CallAsync("aria2.pauseAll", null, ct);
    }

    public async Task UnpauseAllAsync(CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var rpc = _rpc ?? throw new InvalidOperationException("Engine not started.");
        await rpc.CallAsync("aria2.unpauseAll", null, ct);
    }

    public async Task ForcePauseAllAsync(CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var rpc = _rpc ?? throw new InvalidOperationException("Engine not started.");
        await rpc.CallAsync("aria2.forcePauseAll", null, ct);
    }

private async Task WaitForRpcAsync(CancellationToken ct)
    {
        var deadline = Environment.TickCount64 + AppDefaults.RpcReadyTimeoutMs;
        Exception? last = null;
        while (Environment.TickCount64 < deadline)
        {
            ct.ThrowIfCancellationRequested();
            if (_process?.HasExited == true)
                throw new InvalidOperationException("aria2c exited before RPC became ready.");
            try
            {
                _version = await GetVersionAsync(ct);
                return;
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException
                or TaskCanceledException or IOException)
            {
                last = ex;
                await Task.Delay(100, ct);
            }
        }
        throw new TimeoutException($"aria2 RPC not ready within {AppDefaults.RpcReadyTimeoutMs} ms.", last);
    }

    private static StreamWriter OpenLog(string? path)
    {
        if (path is not null)
        {
            var options = new FileStreamOptions
            {
                Mode = FileMode.Create,
                Access = FileAccess.Write,
                Share = FileShare.Read,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
            };
            if (!OperatingSystem.IsWindows())
                options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            try
            {
                return new StreamWriter(new FileStream(path, options)) { NewLine = "\n", AutoFlush = true };
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }
        return new StreamWriter(Stream.Null);
    }

    // aria2 stdout/stderr must always be drained; written to the run's private log file when one is given.
    private static async Task DrainLogsAsync(ChannelReader<string> reader, string? path)
    {
        var writer = OpenLog(path);
        await using (writer)
        {
            var ok = true;
            await foreach (var line in reader.ReadAllAsync())
            {
                if (!ok) continue;
                try { await writer.WriteLineAsync(line); }
                catch (IOException) { ok = false; }
            }
        }
    }

    private static long ReadGlobalLong(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p)) return 0;
        if (p.ValueKind == JsonValueKind.String
            && long.TryParse(p.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
            return v;
        if (p.ValueKind == JsonValueKind.Number && p.TryGetInt64(out var n))
            return n;
        return 0;
    }

    internal static (List<DownloadItem> Waiting, List<DownloadItem> Paused) SplitPaused(List<DownloadItem> items)
    {
        var waiting = new List<DownloadItem>();
        var paused = new List<DownloadItem>();
        foreach (var item in items)
        {
            if (item.State == DownloadState.Paused)
                paused.Add(item);
            else
                waiting.Add(item);
        }
        return (waiting, paused);
    }

}
