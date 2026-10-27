using System.Net.WebSockets;


namespace AriaSharp.Services;

/// <summary>
/// Listens for aria2 JSON-RPC notifications over WebSocket (same port as HTTP RPC).
/// Notifications only arrive on WS: onDownloadStart/Pause/Stop/Complete/Error, onBtDownloadComplete.
/// </summary>
internal sealed class Aria2NotificationListener : IAsyncDisposable
{
    private readonly ChannelWriter<EngineEvent> _events;
    private readonly Uri _uri;
    private readonly CancellationTokenSource _cts = new();
    private ClientWebSocket? _ws;
    private Task? _loop;
    private bool _disposed;

    public Aria2NotificationListener(int port, ChannelWriter<EngineEvent> events)
    {
        _events = events;
        _uri = new Uri($"ws://127.0.0.1:{port}/jsonrpc");
    }

    public async Task StartAsync(CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _ws = new ClientWebSocket { Options = { Proxy = null } };
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);
        await _ws.ConnectAsync(_uri, linked.Token);
        _loop = Task.Run(() => ReceiveLoopAsync(_cts.Token), CancellationToken.None);
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        var ws = _ws;
        if (ws is null) return;

        var buffer = new byte[8 * 1024];
        try
        {
            while (!ct.IsCancellationRequested && ws.State == WebSocketState.Open)
            {
                using var ms = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await ws.ReceiveAsync(buffer, ct);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                        return;
                    }
                    await ms.WriteAsync(buffer.AsMemory(0, result.Count), ct);
                }
                while (!result.EndOfMessage);

                ms.Position = 0;
                try
                {
                    using var doc = await JsonDocument.ParseAsync(ms, cancellationToken: ct);
                    if (TryMap(doc.RootElement, out var ev))
                        _events.TryWrite(ev);
                }
                catch (JsonException)
                {
                    // ignore malformed frames
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (WebSocketException) { }
        catch (IOException) { }
    }

    internal static bool TryMap(JsonElement root, out EngineEvent ev)
    {
        ev = null!;
        if (!root.TryGetProperty("method", out var methodEl) || methodEl.ValueKind != JsonValueKind.String)
            return false;

        var method = methodEl.GetString() ?? "";
        var kind = method switch
        {
            "aria2.onDownloadStart" => EngineEventKind.Started,
            "aria2.onDownloadPause" => EngineEventKind.Paused,
            "aria2.onDownloadStop" => EngineEventKind.Stopped,
            "aria2.onDownloadComplete" => EngineEventKind.Complete,
            "aria2.onBtDownloadComplete" => EngineEventKind.Complete,
            "aria2.onDownloadError" => EngineEventKind.Error,
            _ => (EngineEventKind?)null,
        };
        if (kind is null) return false;

        string? gid = null;
        if (root.TryGetProperty("params", out var paramsEl) && paramsEl.ValueKind == JsonValueKind.Array
            && paramsEl.GetArrayLength() > 0)
        {
            var first = paramsEl[0];
            // params is typically [{"gid":"..."}] or [["gid"]]
            if (first.ValueKind == JsonValueKind.Object && first.TryGetProperty("gid", out var g))
                gid = g.GetString();
            else if (first.ValueKind == JsonValueKind.Array && first.GetArrayLength() > 0)
            {
                var inner = first[0];
                if (inner.ValueKind == JsonValueKind.Object && inner.TryGetProperty("gid", out var g2))
                    gid = g2.GetString();
                else if (inner.ValueKind == JsonValueKind.String)
                    gid = inner.GetString();
            }
        }

        ev = new EngineEvent
        {
            Kind = kind.Value,
            Gid = gid,
            Message = method,
            Timestamp = DateTime.UtcNow,
        };
        return true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try { await _cts.CancelAsync(); }
        catch (ObjectDisposedException) { }

        if (_ws is not null)
        {
            try
            {
                if (_ws.State is WebSocketState.Open or WebSocketState.CloseReceived)
                {
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "shutdown", timeout.Token);
                }
            }
            catch (WebSocketException) { }
            catch (OperationCanceledException) { }
            catch (IOException) { }
            finally
            {
                _ws.Dispose();
                _ws = null;
            }
        }

        if (_loop is not null)
        {
            try { await _loop; }
            catch (OperationCanceledException) { }
            catch (WebSocketException) { }
            catch (IOException) { }
            catch (ObjectDisposedException) { }
        }

        _cts.Dispose();
    }
}
