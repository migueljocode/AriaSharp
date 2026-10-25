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
            Aria2Opt = overrideValues.Aria2Opt ?? baseline.Aria2Opt,
            SpeedLimit = overrideValues.SpeedLimit ?? baseline.SpeedLimit,
            Inputs = overrideValues.Inputs.Length > 0 ? overrideValues.Inputs : baseline.Inputs,
            ExtraOptions = overrideValues.ExtraOptions.Length > 0 ? overrideValues.ExtraOptions : baseline.ExtraOptions,
        };
    }
}

public sealed class SettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };
    public Task<AppSettings> LoadAsync(CancellationToken ct)
    {
        var settings = new AppSettings();
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "ariasharp", "settings.json");
        if (!File.Exists(path)) return Task.FromResult(settings);
        try
        {
            var json = File.ReadAllText(path, System.Text.Encoding.UTF8);
            var loaded = JsonSerializer.Deserialize<AppSettings>(json, Options);
            if (loaded != null) settings = loaded;
        }
        catch (JsonException) { }
        return Task.FromResult(settings);
    }
    public Task SaveAsync(AppSettings settings, CancellationToken ct)
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "ariasharp");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "settings.json");
        File.WriteAllText(path, JsonSerializer.Serialize(settings, Options), System.Text.Encoding.UTF8);
        return Task.CompletedTask;
    }
}