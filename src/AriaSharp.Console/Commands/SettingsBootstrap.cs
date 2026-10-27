
namespace AriaSharp.App;

/// <summary>
/// Layers defaults ← config file ← environment ← CLI into a concrete <see cref="AppSettings"/>.
/// </summary>
internal static class SettingsBootstrap
{
    public static async Task<(AppSettings Settings, SettingsOverlay Cli)> LoadAsync(string[] args)
    {
        var cli = ArgParser.Parse(args);

        var store = new SettingsStore();
        var configFile = cli.ConfigFile ?? AppPaths.SettingsFile;
        SettingsOverlay configOverlay;
        try
        {
            configOverlay = await store.LoadOverlayAsync(configFile, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or DirectoryNotFoundException or IOException)
        {
            await System.Console.Error.WriteLineAsync(
                    $"Warning: could not read config file {configFile} ({ex.Message})")
                .ConfigureAwait(false);
            configOverlay = new SettingsOverlay();
        }

        var envOverlay = EnvOverlay.FromEnvironment();
        var merger = new SettingsMerger();
        var settings = merger.Merge(new AppSettings(), configOverlay, envOverlay, cli);

        if (cli.NoInstall == true)
        {
            settings.InstallDeps = false;
            settings.InstallAria2 = false;
        }

        if (settings.Yes)
        {
            settings.KeepListFiles = true;
            settings.Resume = true;
        }

        if (settings.Force)
            settings.AllowOverwrite = true;

        return (settings, cli);
    }
}
