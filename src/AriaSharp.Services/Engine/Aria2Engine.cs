using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Channels;

using AriaSharp.Models;

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
    private Aria2RpcClient? _rpc;
    private Aria2NotificationListener? _notifications;
    private string _version = string.Empty;
    private bool _disposed;

    public ChannelReader<EngineEvent> Events => _channel.Reader;

    public async Task StartAsync(EngineSettings settings, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_process is not null)
            throw new InvalidOperationException("Engine already started.");

        await WriteConfAsync(settings.ConfFile, settings.Secret, settings.Port, ct).ConfigureAwait(false);

        var logChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });
        _ = DrainLogsAsync(logChannel.Reader, ct);

        _process = new Aria2Process(settings.Aria2Path, settings.Args.ToArray(), logChannel.Writer);

        var http = new HttpClient
        {
            BaseAddress = new Uri($"http://127.0.0.1:{settings.Port}/"),
            Timeout = TimeSpan.FromSeconds(10),
        };
        _rpc = new Aria2RpcClient(http, settings.Secret);

        await WaitForRpcAsync(ct).ConfigureAwait(false);

        _notifications = new Aria2NotificationListener(settings.Port, _channel.Writer);
        try
        {
            await _notifications.StartAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is System.Net.WebSockets.WebSocketException or HttpRequestException or IOException or TimeoutException)
        {
            // Notifications are best-effort; HTTP polling still works.
            await _notifications.DisposeAsync().ConfigureAwait(false);
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
        ], ct).ConfigureAwait(false);

        var active = MapItems(ExtractMulticallResult(multicall, 0));
        var waiting = MapItems(ExtractMulticallResult(multicall, 1));
        var stoppedRaw = MapItems(ExtractMulticallResult(multicall, 2));
        var global = ExtractMulticallResult(multicall, 3);

        var complete = new List<DownloadItem>();
        var failed = new List<DownloadItem>();
        var paused = new List<DownloadItem>();
        var interrupted = new List<DownloadItem>();
        var stopped = new List<DownloadItem>();

        foreach (var item in stoppedRaw)
        {
            switch (item.State)
            {
                case DownloadState.Complete: complete.Add(item); break;
                case DownloadState.Failed: failed.Add(item); break;
                case DownloadState.Paused: paused.Add(item); break;
                case DownloadState.Interrupted: interrupted.Add(item); break;
                default: stopped.Add(item); break;
            }
        }

        long numActive = 0, numWaiting = 0, dlSpeed = 0, ulSpeed = 0;
        if (global.ValueKind == JsonValueKind.Object)
        {
            numActive = ReadLong(global, "numActive");
            numWaiting = ReadLong(global, "numWaiting");
            dlSpeed = ReadLong(global, "downloadSpeed");
            ulSpeed = ReadLong(global, "uploadSpeed");
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
            Skipped = Array.Empty<DownloadItem>(),
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

    public async Task SaveSessionAsync(CancellationToken ct)
    {
        var rpc = _rpc ?? throw new InvalidOperationException("Engine not started.");
        await rpc.CallAsync("aria2.saveSession", null, ct).ConfigureAwait(false);
    }

    public async Task ShutdownAsync(CancellationToken ct)
    {
        if (_rpc is null) return;
        try
        {
            await _rpc.CallAsync("aria2.shutdown", null, ct).ConfigureAwait(false);
        }
        catch (HttpRequestException) { }
        catch (InvalidOperationException) { }
        catch (TaskCanceledException) { }
    }

    public async Task<long> AddUriAsync(string[] uris, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(uris);
        var rpc = _rpc ?? throw new InvalidOperationException("Engine not started.");
        await rpc.CallAsync("aria2.addUri", [uris], ct).ConfigureAwait(false);
        return 0L;
    }

    public async Task<string> GetVersionAsync(CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_version)) return _version;
        var rpc = _rpc ?? throw new InvalidOperationException("Engine not started.");
        var result = await rpc.CallAsync("aria2.getVersion", null, ct).ConfigureAwait(false);
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
                try { await ShutdownAsync(CancellationToken.None).ConfigureAwait(false); }
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
                await _notifications.DisposeAsync().ConfigureAwait(false);
            if (_process is not null)
                await _process.DisposeAsync().ConfigureAwait(false);
            if (_rpc is not null)
                await _rpc.DisposeAsync().ConfigureAwait(false);
            _channel.Writer.TryComplete();
        }
    }

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

    private static async Task WriteConfAsync(string path, string secret, int port, CancellationToken ct)
    {
        var options = new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.Write,
            Share = FileShare.None,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
        };
        if (!OperatingSystem.IsWindows())
            options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;

        var stream = new FileStream(path, options);
        await using (stream.ConfigureAwait(false))
        {
            var writer = new StreamWriter(stream) { NewLine = "\n" };
            await using (writer.ConfigureAwait(false))
            {
                await writer.WriteLineAsync($"rpc-secret={secret}").ConfigureAwait(false);
                await writer.WriteLineAsync("enable-rpc=true").ConfigureAwait(false);
                await writer.WriteLineAsync("rpc-listen-all=false").ConfigureAwait(false);
                await writer.WriteLineAsync($"rpc-listen-port={port}").ConfigureAwait(false);
                await writer.FlushAsync(ct).ConfigureAwait(false);
            }
        }
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
                _version = await GetVersionAsync(ct).ConfigureAwait(false);
                return;
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException
                or TaskCanceledException or IOException)
            {
                last = ex;
                await Task.Delay(100, ct).ConfigureAwait(false);
            }
        }
        throw new TimeoutException($"aria2 RPC not ready within {AppDefaults.RpcReadyTimeoutMs} ms.", last);
    }

    private static async Task DrainLogsAsync(ChannelReader<string> reader, CancellationToken ct)
    {
        try
        {
            await foreach (var _ in reader.ReadAllAsync(ct).ConfigureAwait(false)) { }
        }
        catch (OperationCanceledException) { }
    }

    private static JsonElement ExtractMulticallResult(JsonElement multicall, int index)
    {
        if (multicall.ValueKind != JsonValueKind.Array || index >= multicall.GetArrayLength())
            return default;
        var entry = multicall[index];
        if (entry.ValueKind == JsonValueKind.Array && entry.GetArrayLength() > 0)
            return entry[0];
        return default;
    }

    private static List<DownloadItem> MapItems(JsonElement array)
    {
        var list = new List<DownloadItem>();
        if (array.ValueKind != JsonValueKind.Array) return list;

        foreach (var el in array.EnumerateArray())
        {
            if (el.ValueKind != JsonValueKind.Object) continue;
            var status = ReadString(el, "status");
            var state = status switch
            {
                "active" => DownloadState.Active,
                "waiting" => DownloadState.Waiting,
                "paused" => DownloadState.Paused,
                "complete" => DownloadState.Complete,
                "error" => DownloadState.Failed,
                "removed" => DownloadState.Interrupted,
                _ => DownloadState.Waiting,
            };

            var name = "";
            if (el.TryGetProperty("files", out var files)
                && files.ValueKind == JsonValueKind.Array
                && files.GetArrayLength() > 0)
            {
                var f0 = files[0];
                if (f0.TryGetProperty("path", out var pathEl))
                    name = Path.GetFileName(pathEl.GetString() ?? "") ?? "";
            }

            list.Add(new DownloadItem
            {
                Gid = ReadString(el, "gid"),
                Name = name,
                Status = status,
                State = state,
                Directory = ReadString(el, "dir"),
                CompletedLength = ReadLong(el, "completedLength"),
                TotalLength = ReadLong(el, "totalLength"),
                DownloadSpeed = ReadLong(el, "downloadSpeed"),
                Connections = (int)ReadLong(el, "connections"),
                ErrorCode = (int)ReadLong(el, "errorCode"),
                ErrorMessage = string.IsNullOrEmpty(ReadString(el, "errorMessage"))
                    ? null
                    : ReadString(el, "errorMessage"),
            });
        }
        return list;
    }

    private static string ReadString(JsonElement el, string name) =>
        el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String
            ? p.GetString() ?? ""
            : "";

    private static long ReadLong(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p)) return 0;
        if (p.ValueKind == JsonValueKind.String
            && long.TryParse(p.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
            return v;
        if (p.ValueKind == JsonValueKind.Number && p.TryGetInt64(out var n))
            return n;
        return 0;
    }
}
