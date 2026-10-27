namespace AriaSharp.App;

/// <summary>
/// Standalone --install-deps / --install-aria2 path (no download inputs required).
/// </summary>
internal static class InstallCommand
{
    public static async Task<int> RunAsync(AppSettings settings)
    {
        await Console.Out.WriteLineAsync("Installing prerequisites…");
        var installAria2 = settings.InstallDeps || settings.InstallAria2;
        var nonInteractive = Console.IsInputRedirected;
        var (ok, message) = await PrerequisiteInstaller.InstallDepsAsync(
            installAria2, nonInteractive, CancellationToken.None);
        await Console.Out.WriteLineAsync(message);
        if (!ok)
        {
            await Console.Error.WriteLineAsync("Error: one or more required packages failed to install.");
            return (int)ExitCode.Failure;
        }

        await Console.Out.WriteLineAsync("Done.");
        return (int)ExitCode.Ok;
    }
}
