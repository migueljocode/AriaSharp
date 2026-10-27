using AriaSharp.Models;
using AriaSharp.Services;

namespace AriaSharp.App;

/// <summary>
/// Standalone --install-deps / --install-aria2 path (no download inputs required).
/// </summary>
internal static class InstallCommand
{
    public static async Task<int> RunAsync(AppSettings settings)
    {
        await System.Console.Out.WriteLineAsync("Installing prerequisites…").ConfigureAwait(false);
        var installAria2 = settings.InstallDeps || settings.InstallAria2;
        var (ok, message) = await PrerequisiteInstaller.InstallDepsAsync(installAria2, CancellationToken.None)
            .ConfigureAwait(false);
        await System.Console.Out.WriteLineAsync(message).ConfigureAwait(false);
        if (!ok)
        {
            await System.Console.Error.WriteLineAsync("Error: one or more required packages failed to install.").ConfigureAwait(false);
            return (int)ExitCode.Failure;
        }
        await System.Console.Out.WriteLineAsync("Done.").ConfigureAwait(false);
        return (int)ExitCode.Ok;
    }
}
