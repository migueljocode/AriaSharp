using System.IO;
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
    public Task<AppSettings> LoadAsync(CancellationToken ct) => LoadAsync(ct, null!);

    public async Task<AppSettings> LoadAsync(CancellationToken ct, string? path = null)
    {
        path ??= AppPaths.SettingsFile;
        if (!File.Exists(path)) return new AppSettings();
        try
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
            var loaded = await JsonSerializer.DeserializeAsync(stream, AppSettingsJsonContext.Default.AppSettings, ct).ConfigureAwait(false);
            return loaded ?? new AppSettings();
        }
        catch (JsonException ex)
        {
            await System.Console.Error.WriteLineAsync($"Warning: settings file corrupt ({ex.Message}); using defaults").ConfigureAwait(false);
            return new AppSettings();
        }
    }

    public Task SaveAsync(AppSettings settings, CancellationToken ct) => SaveAsync(settings, ct, null!);

    public async Task SaveAsync(AppSettings settings, CancellationToken ct, string? path = null)
    {
        path ??= AppPaths.SettingsFile;
        var dir = Path.GetDirectoryName(path)!;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        var tmp = Path.Join(dir, $"settings-{Guid.NewGuid():N}.tmp");
        try
        {
            {
                await using var stream = new FileStream(tmp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
                await JsonSerializer.SerializeAsync(stream, settings, AppSettingsJsonContext.Default.AppSettings, ct).ConfigureAwait(false);
                await stream.FlushAsync(ct).ConfigureAwait(false);
            }
            File.Move(tmp, path, overwrite: true);
        }
        catch
        {
            try { File.Delete(tmp); } catch { }
            throw;
        }
    }
}
