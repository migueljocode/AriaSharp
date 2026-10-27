namespace AriaSharp.Services;

/// <summary>Public diagnostics surface so Console need not use ProcessRunner.</summary>
public static class Aria2Diagnostics
{
    public static async Task<(bool Available, string Detail)> ProbeManagerAsync(
        PackageManagerEntry mgr,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(mgr);
        try
        {
            var probe = await ProcessRunner.RunAsync(mgr.ProbeFileName, mgr.ProbeArgs, ct);
            var avail = PackageManagerCatalog.IsPackageAvailable(
                mgr.ProbeKind, probe.ExitCode, probe.StdOut, probe.StdErr);
            return (avail, avail ? "aria2 package visible" : "not available");
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or System.ComponentModel.Win32Exception)
        {
            return (false, $"probe error ({ex.Message})");
        }
    }

    public static async Task<(bool Ok, string FirstLine, bool Https)> VersionAsync(string aria2Path, CancellationToken ct)
    {
        var ver = await ProcessRunner.RunAsync(aria2Path, "--version", ct);
        var first = ver.StdOut.Split('\n')[0];
        var https = ver.StdOut.Contains("HTTPS", StringComparison.OrdinalIgnoreCase);
        return (ver.ExitCode == 0, first, https);
    }
}
