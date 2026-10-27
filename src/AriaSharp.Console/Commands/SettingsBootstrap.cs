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
            await Console.Error.WriteLineAsync(
                    $"Warning: could not read config file {configFile} ({ex.Message})")
                .ConfigureAwait(false);
            configOverlay = new();
        }

        var settings = new SettingsMerger().Merge(new AppSettings(), configOverlay, EnvOverlay.FromEnvironment(), cli);

        if (cli.NoInstall is true)
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
