
namespace AriaSharp.App;

/// <summary>Diagnostics: OS, network, managers, aria2 path/version/HTTPS.</summary>
internal static class DoctorCommand
{
    public static async Task<int> RunAsync(CancellationToken ct = default)
    {
        var lines = new List<string>
        {
            $"OS: {Environment.OSVersion}",
            $"RID: {System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier}",
            $"user: {Environment.UserName}",
            $"tools: {AppPaths.ToolsBinDirectory}",
        };

        var (netOk, netDetail) = await NetworkProbe.CheckAsync(ct);
        lines.Add($"network: {(netOk ? "ok" : "fail")} ({netDetail})");

        foreach (var mgr in PackageManagerCatalog.Aria2Managers.Where(m => m.IsOsMatch()))
        {
            var probe = await ProcessRunner.RunAsync(mgr.Id, mgr.ProbeArgs, ct);
            lines.Add($"manager {mgr.DisplayName}: {(probe.ExitCode == 0 ? "aria2 package visible" : "not available")}");
        }

        try
        {
            var path = await new Aria2Locator().LocateAsync(ct);
            lines.Add($"aria2c: {path}");
            var ver = await ProcessRunner.RunAsync(path, "--version", ct);
            lines.Add(ver.StdOut.Split('\n')[0]);
            lines.Add(ver.StdOut.Contains("HTTPS", StringComparison.OrdinalIgnoreCase)
                ? "HTTPS: yes"
                : "HTTPS: no or unknown");
        }
        catch (FileNotFoundException)
        {
            lines.Add("aria2c: not found");
        }

        await Console.Out.WriteLineAsync(string.Join(Environment.NewLine, lines));
        return (int)ExitCode.Ok;
    }
}
