using System.Text.Json;
using AriaSharp.Models;

namespace AriaSharp.Services;

public interface ISettingsMerger
{
    AppSettings Merge(AppSettings baseline, AppSettings overrideValues);
}

public sealed class SettingsMerger : ISettingsMerger
{
    public AppSettings Merge(AppSettings baseline, AppSettings overrideValues)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(overrideValues);
        return new AppSettings
        {
            MaxConcurrentDownloads = overrideValues.MaxConcurrentDownloads != AppDefaults.MaxConcurrentDownloads ? overrideValues.MaxConcurrentDownloads : baseline.MaxConcurrentDownloads,
            Split = overrideValues.Split != AppDefaults.Split ? overrideValues.Split : baseline.Split,
            MaxConnectionPerServer = overrideValues.MaxConnectionPerServer != AppDefaults.MaxConnectionPerServer ? overrideValues.MaxConnectionPerServer : baseline.MaxConnectionPerServer,
            AutoFileRenaming = overrideValues.AutoFileRenaming || baseline.AutoFileRenaming,
            AllowOverwrite = overrideValues.AllowOverwrite || baseline.AllowOverwrite,
            SaveSession = overrideValues.SaveSession && baseline.SaveSession,
            SaveSessionInterval = overrideValues.SaveSessionInterval != AppDefaults.SaveSessionInterval ? overrideValues.SaveSessionInterval : baseline.SaveSessionInterval,
            RefreshIntervalMs = overrideValues.RefreshIntervalMs != AppDefaults.RefreshIntervalMs ? overrideValues.RefreshIntervalMs : baseline.RefreshIntervalMs,
            Inline = overrideValues.Inline || baseline.Inline,
            Plain = overrideValues.Plain || baseline.Plain,
            NoColor = overrideValues.NoColor || baseline.NoColor,
            NoPager = overrideValues.NoPager || baseline.NoPager,
            Verbose = overrideValues.Verbose || baseline.Verbose,
            Yes = overrideValues.Yes || baseline.Yes,
            Force = overrideValues.Force || baseline.Force,
            KeepListFiles = overrideValues.KeepListFiles || baseline.KeepListFiles,
            Resume = overrideValues.Resume || baseline.Resume,
            ProbeSizes = overrideValues.ProbeSizes || baseline.ProbeSizes,
            InstallDeps = overrideValues.InstallDeps || baseline.InstallDeps,
            InstallAria2 = overrideValues.InstallAria2 || baseline.InstallAria2,
            OutputDir = overrideValues.OutputDir ?? baseline.OutputDir,
            SessionFile = overrideValues.SessionFile ?? baseline.SessionFile,
            ConfigFile = overrideValues.ConfigFile ?? baseline.ConfigFile,
            Aria2Path = overrideValues.Aria2Path ?? baseline.Aria2Path,
            SpeedLimit = overrideValues.SpeedLimit ?? baseline.SpeedLimit,
            Inputs = overrideValues.Inputs.Count > 0 ? overrideValues.Inputs : baseline.Inputs,
            ExtraOptions = overrideValues.ExtraOptions.Count > 0 ? overrideValues.ExtraOptions : baseline.ExtraOptions,
        };
    }
}

public sealed class SettingsStore : ISettingsStore
{
    public async Task<AppSettings> LoadAsync(CancellationToken ct)
    {
        var settings = new AppSettings();
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "ariasharp", "settings.json");
        if (!File.Exists(path)) return settings;
        try
        {
            var json = await File.ReadAllTextAsync(path, System.Text.Encoding.UTF8, ct).ConfigureAwait(false);
            var loaded = JsonSerializer.Deserialize<AppSettings>(json, AppSettingsJsonContext.Default.AppSettings);
            if (loaded != null) settings = loaded;
        }
        catch (JsonException) { }
        return settings;
    }
    public async Task SaveAsync(AppSettings settings, CancellationToken ct)
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "ariasharp");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "settings.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(settings, AppSettingsJsonContext.Default.AppSettings), System.Text.Encoding.UTF8, ct).ConfigureAwait(false);
    }
}