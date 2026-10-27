namespace AriaSharp.App;

/// <summary>Locate aria2c, optionally installing into the user tools dir.</summary>
internal static class Aria2Bootstrap
{
    public static async Task<(string? Path, int? ExitCode)> LocateOrInstallAsync(
        AppSettings settings,
        CancellationToken ct)
    {
        try
        {
            return (await new Aria2Locator(settings.Aria2Path).LocateAsync(ct), null);
        }
        catch (FileNotFoundException ex)
        {
            var mayInstall = !settings.NoInstall
                && (settings.InstallDeps || settings.InstallAria2 || settings.Yes);
            if (!mayInstall)
            {
                await Console.Error.WriteLineAsync($"Error: {ex.Message}");
                await Console.Error.WriteLineAsync(
                    "Hint: pass -y / --install-deps / --install-aria2 to install, or --aria2-path.");
                return (null, (int)ExitCode.Failure);
            }

            var installed = await PrerequisiteInstaller.InstallAria2Async(ct);
            if (!installed)
            {
                await Console.Error.WriteLineAsync($"Error: {ex.Message}");
                return (null, (int)ExitCode.Failure);
            }

            try
            {
                return (await new Aria2Locator(settings.Aria2Path).LocateAsync(ct), null);
            }
            catch (FileNotFoundException retryEx)
            {
                await Console.Error.WriteLineAsync(
                    $"Error: install reported success but aria2c still missing ({retryEx.Message})");
                return (null, (int)ExitCode.Failure);
            }
        }
    }
}
