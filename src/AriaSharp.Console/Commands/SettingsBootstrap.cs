namespace AriaSharp.App;

using System.Collections;

/// <summary>
/// Layers defaults ← config file ← environment ← CLI into a concrete <see cref="AppSettings"/>.
/// </summary>
internal static class SettingsBootstrap
{
    public static async Task<(AppSettings Settings, SettingsOverlay Cli)> LoadAsync(string[] args)
    {
        var cli = args.Parse();
        var store = new SettingsStore();
        var configFile = cli.ConfigFile ?? AppPaths.SettingsFile;

        SettingsOverlay configOverlay;
        bool configFound;
        try
        {
            configOverlay = await store.LoadOverlayAsync(configFile, CancellationToken.None);
            configFound = true;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or DirectoryNotFoundException or IOException)
        {
            await Console.Error.WriteLineAsync(
                    $"Warning: could not read config file {configFile} ({ex.Message})");
            configOverlay = new();
            configFound = false;
        }

        var envOverlay = EnvOverlay.FromEnvironment();

        var settings = new SettingsMerger().Merge(new AppSettings(), configOverlay, envOverlay, cli);

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

        if (cli.Verbose is true || cli.PrintConfig is true)
        {
            await WriteDiagnosticsAsync(configFile, configFound, configOverlay, envOverlay, cli, settings);
        }

        return (settings, cli);
    }

    private static async Task WriteDiagnosticsAsync(
        string configFile, bool configFound, SettingsOverlay configOverlay, SettingsOverlay envOverlay, SettingsOverlay cli, AppSettings settings)
    {
        await Console.Error.WriteLineAsync($"config: {configFile} ({(configFound ? "found" : "absent")})");

        foreach (DictionaryEntry kv in Environment.GetEnvironmentVariables())
        {
            if (kv.Key is string key && key.StartsWith("ARIASHARP_", StringComparison.OrdinalIgnoreCase))
                await Console.Error.WriteLineAsync($"env: {key}");
        }

        var jobsSource = DetermineJobsSource(cli, envOverlay, configOverlay);
        await Console.Error.WriteLineAsync($"source: jobs={settings.MaxConcurrentDownloads} from {jobsSource}");
    }

    private static string DetermineJobsSource(
        SettingsOverlay cli, SettingsOverlay env, SettingsOverlay config)
    {
        if (cli.MaxConcurrentDownloads.HasValue) return "cli";
        if (env.MaxConcurrentDownloads.HasValue) return "env";
        if (config.MaxConcurrentDownloads.HasValue) return "config";
        return "default";
    }
}
