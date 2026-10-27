using System.Net.Sockets;

namespace AriaSharp.Services;

/// <summary>Light connectivity check before download/install.</summary>
public static class NetworkProbe
{
    public static async Task<(bool Ok, string Detail)> CheckAsync(CancellationToken ct)
    {
        var overrideEnv = Environment.GetEnvironmentVariable("ARIASHARP_NETWORK");
        if (string.Equals(overrideEnv, "offline", StringComparison.OrdinalIgnoreCase))
            return (false, "ARIASHARP_NETWORK=offline");
        if (string.Equals(overrideEnv, "online", StringComparison.OrdinalIgnoreCase))
            return (true, "ARIASHARP_NETWORK=online");

        if (HasProxyEnv())
            return (true, "proxy env set (HTTPS_PROXY/http_proxy/ALL_PROXY)");

        var hosts = new[] { "1.1.1.1", "8.8.8.8" };
        var tasks = hosts.Select(h => TryTcpAsync(h, 443, ct)).ToArray();
        var results = await Task.WhenAll(tasks);
        for (var i = 0; i < results.Length; i++)
        {
            if (results[i])
                return (true, $"tcp 443 → {hosts[i]}");
        }

        try
        {
            var entry = await System.Net.Dns.GetHostEntryAsync("github.com", ct);
            if (entry.AddressList.Length > 0)
                return (true, "dns github.com");
        }
        catch (Exception ex) when (ex is SocketException or OperationCanceledException)
        {
            return (false, "no network (TCP/DNS failed)");
        }

        return (false, "no network");
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
