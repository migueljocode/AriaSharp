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

        foreach (var host in new[] { "1.1.1.1", "8.8.8.8" })
        {
            try
            {
                using var client = new TcpClient();
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
                linked.CancelAfter(TimeSpan.FromSeconds(3));
                await client.ConnectAsync(host, 443, linked.Token);
                return (true, $"tcp 443 → {host}");
            }
            catch (Exception ex) when (ex is SocketException or OperationCanceledException or IOException)
            {
                // try next
            }
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
}
