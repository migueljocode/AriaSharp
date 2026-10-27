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

            var autoYes = settings.Yes || settings.InstallDeps || settings.InstallAria2;
            Func<string, bool>? confirm = null;
            if (!autoYes && !Console.IsInputRedirected && !Console.IsOutputRedirected)
            {
                confirm = cmd =>
                {
                    Console.Error.WriteLine($"About to run: {cmd}");
                    Console.Error.Write("Proceed? [y/N] ");
                    var line = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(line))
                        return false;
                    var c = line.Trim()[0];
                    return c is 'y' or 'Y';
                };
            }
            else if (!autoYes)
            {
                await Console.Error.WriteLineAsync(
                    "Error: non-interactive session requires -y/--yes to install dependencies.");
                return (null, (int)ExitCode.Failure);
            }

            var (ok, message, kind) = await PrerequisiteInstaller.InstallAria2Async(ct, autoYes, confirm);
            if (!ok)
            {
                await Console.Error.WriteLineAsync($"Error: {message} ({kind})");
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
