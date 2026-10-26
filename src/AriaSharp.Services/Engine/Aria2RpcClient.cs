using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AriaSharp.Services;

/// <summary>
/// Minimal aria2 JSON-RPC 2.0 client over HTTP. No third-party packages; AOT-friendly with
/// source-generated System.Text.Json. WebSocket notifications are a later slice.
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

    public async Task<JsonElement> CallAsync(string method, object?[]? parameters, CancellationToken ct)
    {
        var id = Interlocked.Increment(ref _id);
        object[] paramsWithToken;
        if (parameters is null || parameters.Length == 0)
            paramsWithToken = [_tokenParam];
        else
        {
            paramsWithToken = new object[parameters.Length + 1];
            paramsWithToken[0] = _tokenParam;
            parameters.CopyTo(paramsWithToken, 1);
        }

        var request = new RpcRequest
        {
            JsonRpc = "2.0",
            Id = id.ToString(CultureInfo.InvariantCulture),
            Method = method,
            Params = paramsWithToken,
        };

        using var response = await _http.PostAsJsonAsync(
            "jsonrpc",
            request,
            Aria2RpcJsonContext.Default.RpcRequest,
            ct).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        await using (stream.ConfigureAwait(false))
        {
            var rpc = await JsonSerializer.DeserializeAsync(stream, Aria2RpcJsonContext.Default.RpcResponse, ct)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException("Empty JSON-RPC response.");

            if (rpc.Error is { } err)
                throw new InvalidOperationException($"aria2 RPC {method}: [{err.Code}] {err.Message}");

            return rpc.Result ?? throw new InvalidOperationException($"aria2 RPC {method}: missing result.");
        }
    }

    public async Task<JsonElement> MulticallAsync(IReadOnlyList<(string Method, object?[]? Params)> calls, CancellationToken ct)
    {
        // aria2 system.multicall: array of {methodName, params}. Token is first element of each
        // method's params (aria2 does not inject the secret for multicall entries).
        var entries = new MulticallEntry[calls.Count];
        for (var i = 0; i < calls.Count; i++)
        {
            var (method, p) = calls[i];
            object[] inner;
            if (p is null || p.Length == 0)
                inner = [_tokenParam];
            else
            {
                inner = new object[p.Length + 1];
                inner[0] = _tokenParam;
                p.CopyTo(inner, 1);
            }
            entries[i] = new MulticallEntry { MethodName = method, Params = inner };
        }

        // system.multicall itself does not take the token as a separate first arg when using
        // the structured form; pass the entries array only.
        return await CallAsync("system.multicall", [entries], ct).ConfigureAwait(false);
    }

    public ValueTask DisposeAsync()
    {
        _http.Dispose();
        return ValueTask.CompletedTask;
    }

    internal sealed class RpcRequest
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonPropertyName("id")]
        public string Id { get; set; } = "1";

        [JsonPropertyName("method")]
        public string Method { get; set; } = "";

        [JsonPropertyName("params")]
        public object[]? Params { get; set; }
    }

    internal sealed class RpcResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("result")]
        public JsonElement? Result { get; set; }

        [JsonPropertyName("error")]
        public RpcError? Error { get; set; }
    }

    internal sealed class RpcError
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = "";
    }

    internal sealed class MulticallEntry
    {
        [JsonPropertyName("methodName")]
        public string MethodName { get; set; } = "";

        [JsonPropertyName("params")]
        public object[] Params { get; set; } = [];
    }
}

[JsonSerializable(typeof(Aria2RpcClient.RpcRequest))]
[JsonSerializable(typeof(Aria2RpcClient.RpcResponse))]
[JsonSerializable(typeof(Aria2RpcClient.MulticallEntry))]
[JsonSerializable(typeof(Aria2RpcClient.MulticallEntry[]))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class Aria2RpcJsonContext : JsonSerializerContext;
