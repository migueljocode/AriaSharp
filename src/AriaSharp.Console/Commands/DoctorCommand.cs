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
            $"privileged: {PrerequisiteInstaller.IsPrivileged()}",
            $"tools: {AppPaths.ToolsBinDirectory}",
        };

        var (netOk, netDetail) = await NetworkProbe.CheckAsync(ct);
        lines.Add($"network: {(netOk ? "ok" : "fail")} ({netDetail})");

        foreach (var mgr in PackageManagerCatalog.Aria2Managers.Where(static m => m.IsOsMatch()))
        {
            var (_, detail) = await Aria2Diagnostics.ProbeManagerAsync(mgr, ct);
            lines.Add($"manager {mgr.DisplayName}: {detail}");
        }

        var exit = (int)ExitCode.Ok;
        try
        {
            var path = await new Aria2Locator().LocateAsync(ct);
            lines.Add($"aria2c: {path}");
            var (ok, first, https) = await Aria2Diagnostics.VersionAsync(path, ct);
            lines.Add(first);
            if (!ok || !https)
            {
                lines.Add(https ? "HTTPS: yes" : "HTTPS: no or unknown");
                exit = (int)ExitCode.Failure;
            }
            else
            {
                lines.Add("HTTPS: yes");
            }
        }
        catch (FileNotFoundException)
        {
            lines.Add("aria2c: not found");
            exit = (int)ExitCode.Failure;
        }

        await Console.Out.WriteLineAsync(string.Join(Environment.NewLine, lines));
        return exit;
    }
}
