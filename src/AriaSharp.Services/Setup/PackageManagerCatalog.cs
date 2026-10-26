using System.ComponentModel;
using System.Diagnostics;
using AriaSharp.Models;

namespace AriaSharp.Services;

public sealed class PrerequisiteInstaller : IPrerequisiteInstaller
{
    public async Task<bool> InstallAsync(string package, CancellationToken ct)
    {
        var pkg = package switch { "wl-clipboard" => "wl-clipboard", "xclip" => "xclip", "xsel" => "xsel", _ => package };
        return (await RunAsync("apt-get", $"install -y {pkg}", ct).ConfigureAwait(false)) == 0;
    }
    public async Task<bool> IsInstalledAsync(string package, CancellationToken ct)
    {
        var command = package switch { "wl-clipboard" => "wl-paste", "xclip" => "xclip", "xsel" => "xsel", _ => package };
        return (await RunAsync("which", command, ct).ConfigureAwait(false)) == 0;
    }
    public static async Task<bool> InstallAria2Async(CancellationToken ct)
    {
        var script = "curl -fsSL https://aria2.github.io/dl/aria2-1.37.0.tar.gz -o /tmp/aria2.tar.gz && tar -xzf /tmp/aria2.tar.gz -C /tmp && cd /tmp/aria2-1.37.0 && ./configure --prefix=/usr/local && make && make install";
        return (await RunAsync("bash", $"-c \"{script}\"", ct).ConfigureAwait(false)) == 0;
    }
    private static async Task<int> RunAsync(string fileName, string arguments, CancellationToken ct)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo { FileName = fileName, Arguments = arguments, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true });
            if (process == null) return -1;
            await process.WaitForExitAsync(ct).ConfigureAwait(false);
            return process.ExitCode;
        }
        catch (InvalidOperationException) { return -1; }
        catch (Win32Exception) { return -1; }
    }
}

public sealed class Aria2Manifest
{
    public static readonly string Version = "1.37.0";
    public static readonly string TarballUrl = "https://aria2.github.io/dl/aria2-1.37.0.tar.gz";
    public static readonly string WindowsZipUrl = "https://github.com/aria2/aria2/releases/download/release-1.37.0/aria2-1.37.0-win-64bit.zip";
}

public sealed class PackageManagerCatalog
{
    public static readonly Dictionary<string, string> Linux = new() { ["wl-clipboard"] = "wl-clipboard", ["xclip"] = "xclip", ["xsel"] = "xsel" };
    public static readonly Dictionary<string, string> Mac = new() { ["wl-clipboard"] = "wl-clipboard", ["xclip"] = "xclip", ["xsel"] = "xsel" };
}

public sealed class FailureClassifier
{
    public static string Classify(Exception ex) => ex switch { TimeoutException => "timeout", System.Net.WebException => "network", System.Net.Sockets.SocketException => "network", IOException => "io", _ => "unknown" };
}

public sealed class NetworkProbe
{
    public static async Task<bool> CanReachLoopbackAsync(int port, CancellationToken ct)
    {
        try { using var client = new System.Net.Sockets.TcpClient(); await client.ConnectAsync("127.0.0.1", port, ct).ConfigureAwait(false); return true; }
        catch (System.Net.Sockets.SocketException) { return false; }
        catch (System.Net.NetworkInformation.PingException) { return false; }
    }
}

public sealed class Aria2SourceBuilder
{
    public static Uri BuildTarballUrl(string version) => new($"https://aria2.github.io/dl/aria2-{version}.tar.gz");
}