
namespace AriaSharp.Services;

/// <summary>
/// Minimal aria2 JSON-RPC 2.0 client over HTTP. Hand-written UTF-8 JSON for AOT safety.
/// </summary>
internal sealed class Aria2RpcClient : IAsyncDisposable
{
    private readonly HttpClient _http;
    private readonly string _tokenParam;
    private int _id;

    public Aria2RpcClient(HttpClient http, string secret)
    {
        _http = http;
        _tokenParam = "token:" + secret;
    }

    public Task<JsonElement> CallAsync(string method, object?[]? parameters, CancellationToken ct)
    {
        var id = Interlocked.Increment(ref _id).ToString(CultureInfo.InvariantCulture);
        var json = BuildRequest(id, method, parameters);
        return SendAsync(method, json, ct);
    }

    public Task<JsonElement> MulticallAsync(IReadOnlyList<(string Method, object?[]? Params)> calls, CancellationToken ct)
    {
        var id = Interlocked.Increment(ref _id).ToString(CultureInfo.InvariantCulture);
        using var stream = new MemoryStream();
        using (var w = new Utf8JsonWriter(stream))
        {
            w.WriteStartObject();
            w.WriteString("jsonrpc", "2.0");
            w.WriteString("id", id);
            w.WriteString("method", "system.multicall");
            // system.multicall takes one argument: the array of calls (no outer token).
            // Each call's params must start with the token when --rpc-secret is set.
            w.WritePropertyName("params");
            w.WriteStartArray();
            w.WriteStartArray();
            foreach (var (method, p) in calls)
            {
                w.WriteStartObject();
                w.WriteString("methodName", method);
                w.WritePropertyName("params");
                w.WriteStartArray();
                w.WriteStringValue(_tokenParam);
                if (p is not null)
                {
                    foreach (var item in p)
                        WriteValue(w, item);
                }
                w.WriteEndArray();
                w.WriteEndObject();
            }
            w.WriteEndArray();
            w.WriteEndArray();
            w.WriteEndObject();
        }
        return SendAsync("system.multicall", stream.ToArray(), ct);
    }

    public ValueTask DisposeAsync()
    {
        _http.Dispose();
        return ValueTask.CompletedTask;
    }

    private async Task<JsonElement> SendAsync(string method, byte[] json, CancellationToken ct)
    {
        using var content = new ByteArrayContent(json);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        using var response = await _http.PostAsync(new Uri("jsonrpc", UriKind.Relative), content, ct);
        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync(ct);
        await using (stream)
        {
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var root = doc.RootElement;
            if (root.TryGetProperty("error", out var err)
                && err.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
            {
                var code = err.TryGetProperty("code", out var c) ? c.GetInt32() : 0;
                var msg = err.TryGetProperty("message", out var m) ? m.GetString() : "unknown";
                throw new InvalidOperationException($"aria2 RPC {method}: [{code}] {msg}");
            }
            if (!root.TryGetProperty("result", out var result))
                throw new InvalidOperationException($"aria2 RPC {method}: missing result.");
            return result.Clone();
        }
    }

    private byte[] BuildRequest(string id, string method, object?[]? parameters)
    {
        using var stream = new MemoryStream();
        using (var w = new Utf8JsonWriter(stream))
        {
            w.WriteStartObject();
            w.WriteString("jsonrpc", "2.0");
            w.WriteString("id", id);
            w.WriteString("method", method);
            w.WritePropertyName("params");
            w.WriteStartArray();
            w.WriteStringValue(_tokenParam);
            if (parameters is not null)
            {
                foreach (var p in parameters)
                    WriteValue(w, p);
            }
            w.WriteEndArray();
            w.WriteEndObject();
        }
        return stream.ToArray();
    }

    private static void WriteValue(Utf8JsonWriter w, object? value)
    {
        switch (value)
        {
            case null:
                w.WriteNullValue();
                break;
            case string s:
                w.WriteStringValue(s);
                break;
            case int i:
                w.WriteNumberValue(i);
                break;
            case long l:
                w.WriteNumberValue(l);
                break;
            case bool b:
                w.WriteBooleanValue(b);
                break;
            case string[] arr:
                w.WriteStartArray();
                foreach (var s in arr) w.WriteStringValue(s);
                w.WriteEndArray();
                break;
            case int[] arr:
                w.WriteStartArray();
                foreach (var i in arr) w.WriteNumberValue(i);
                w.WriteEndArray();
                break;
            case object[] arr:
                w.WriteStartArray();
                foreach (var o in arr) WriteValue(w, o);
                w.WriteEndArray();
                break;
            default:
                w.WriteStringValue(Convert.ToString(value, CultureInfo.InvariantCulture));
                break;
        }
    }
}
