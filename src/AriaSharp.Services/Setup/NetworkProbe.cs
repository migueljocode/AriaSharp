using System.Net.Sockets;

namespace AriaSharp.Services;

/// <summary>Connectivity check before download/install (proxy, TCP, DNS, optional release HEAD).</summary>
public static class NetworkProbe
{
    public static async Task<(bool Ok, string Detail)> CheckAsync(CancellationToken ct)
    {
        var overrideEnv = Environment.GetEnvironmentVariable("ARIASHARP_NETWORK");
        if (string.Equals(overrideEnv, "offline", StringComparison.OrdinalIgnoreCase))
            return (false, "ARIASHARP_NETWORK=offline");
        if (string.Equals(overrideEnv, "online", StringComparison.OrdinalIgnoreCase))
            return (true, "ARIASHARP_NETWORK=online");

        // Prefer a real reachability check against the release host.
        var (releaseOk, releaseDetail) = await TryReleaseHeadAsync(ct);
        if (releaseOk)
            return (true, releaseDetail);

        if (HasProxyEnv())
            return (true, "proxy env set (HTTPS_PROXY/http_proxy/ALL_PROXY); release HEAD failed: " + releaseDetail);

        var hosts = new[] { "1.1.1.1", "8.8.8.8" };
        var tasks = hosts.Select(h => TryTcpAsync(h, 443, ct)).ToArray();
        var results = await Task.WhenAll(tasks);
        for (var i = 0; i < results.Length; i++)
        {
            if (results[i])
                return (true, $"tcp 443 → {hosts[i]} (release HEAD failed: {releaseDetail})");
        }

        try
        {
            var entry = await System.Net.Dns.GetHostEntryAsync("github.com", ct);
            if (entry.AddressList.Length > 0)
                return (true, "dns github.com (release HEAD failed: " + releaseDetail + ")");
        }
        catch (Exception ex) when (ex is SocketException or OperationCanceledException)
        {
            return (false, "no network (TCP/DNS failed); " + releaseDetail);
        }

        return (false, "no network; " + releaseDetail);
    }

    public static bool HasProxyEnv()
    {
        foreach (var key in new[] { "HTTPS_PROXY", "https_proxy", "HTTP_PROXY", "http_proxy", "ALL_PROXY", "all_proxy" })
        {
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
                return true;
        }
        return false;
    }

    private static async Task<(bool Ok, string Detail)> TryReleaseHeadAsync(CancellationToken ct)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd(AppDefaults.UserAgent);
            using var req = new HttpRequestMessage(HttpMethod.Head, Aria2Manifest.SourceTarballGzUrl);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
            linked.CancelAfter(TimeSpan.FromSeconds(5));
            using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, linked.Token);
            if ((int)resp.StatusCode is >= 200 and < 400 or 403 or 405)
                return (true, $"HEAD github release → {(int)resp.StatusCode}");
            return (false, $"HEAD github release → {(int)resp.StatusCode}");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException or IOException)
        {
            return (false, "HEAD github release failed: " + ex.GetType().Name);
        }
    }

    private static async Task<bool> TryTcpAsync(string host, int port, CancellationToken ct)
    {
        try
        {
            using var client = new TcpClient();
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
            linked.CancelAfter(TimeSpan.FromSeconds(2));
            await client.ConnectAsync(host, port, linked.Token);
            return true;
        }
        catch (Exception ex) when (ex is SocketException or OperationCanceledException or IOException)
        {
            return false;
        }
    }
}
