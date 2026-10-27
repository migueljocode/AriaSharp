using System.IO;


namespace AriaSharp.Services;

/// <summary>
/// Layering contract. A layer is a <see cref="SettingsOverlay"/> (CLI or config file) applied on
/// top of a baseline <see cref="AppSettings"/>. Null fields mean "not set here" — they fall
/// back to the baseline. A non-null field always wins, which is what lets a config file reset a
/// value back to its default (the baseline is rebuilt from defaults first).
/// </summary>
public interface ISettingsMerger
{
    AppSettings Merge(AppSettings baseline, params SettingsOverlay[] layers);
}

public sealed class SettingsMerger : ISettingsMerger
{
    public AppSettings Merge(AppSettings baseline, params SettingsOverlay[] layers)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        layers ??= Array.Empty<SettingsOverlay>();
        var overlay = new SettingsOverlay();
        // later layers win; merge them into one overlay first.
        foreach (var l in layers) ApplyLayer(overlay, l);
        return MergeOne(baseline, overlay);
    }

    private static void ApplyLayer(SettingsOverlay target, SettingsOverlay layer)
    {
        if (layer is null) return;
        if (layer.MaxConcurrentDownloads.HasValue) target.MaxConcurrentDownloads = layer.MaxConcurrentDownloads;
        if (layer.Connections.HasValue) target.Connections = layer.Connections;
        if (layer.Split.HasValue) target.Split = layer.Split;
        if (layer.MaxConnectionPerServer.HasValue) target.MaxConnectionPerServer = layer.MaxConnectionPerServer;
        if (layer.AutoFileRenaming.HasValue) target.AutoFileRenaming = layer.AutoFileRenaming;
        if (layer.AllowOverwrite.HasValue) target.AllowOverwrite = layer.AllowOverwrite;
        if (layer.SaveSession.HasValue) target.SaveSession = layer.SaveSession;
        if (layer.SaveSessionInterval.HasValue) target.SaveSessionInterval = layer.SaveSessionInterval;
        if (layer.RefreshIntervalMs.HasValue) target.RefreshIntervalMs = layer.RefreshIntervalMs;
        if (layer.PollIntervalMs.HasValue) target.PollIntervalMs = layer.PollIntervalMs;
        if (layer.Inline.HasValue) target.Inline = layer.Inline;
        if (layer.Plain.HasValue) target.Plain = layer.Plain;
        if (layer.NoColor.HasValue) target.NoColor = layer.NoColor;
        if (layer.Ascii.HasValue) target.Ascii = layer.Ascii;
        if (layer.NoPager.HasValue) target.NoPager = layer.NoPager;
        if (layer.NoInstall.HasValue) target.NoInstall = layer.NoInstall;
        if (layer.Verbose.HasValue) target.Verbose = layer.Verbose;
        if (layer.Yes.HasValue) target.Yes = layer.Yes;
        if (layer.Force.HasValue) target.Force = layer.Force;
        if (layer.KeepListFiles.HasValue) target.KeepListFiles = layer.KeepListFiles;
        if (layer.Resume.HasValue) target.Resume = layer.Resume;
        if (layer.InstallDeps.HasValue) target.InstallDeps = layer.InstallDeps;
        if (layer.InstallAria2.HasValue) target.InstallAria2 = layer.InstallAria2;
        if (layer.Help.HasValue) target.Help = layer.Help;
        if (layer.Version.HasValue) target.Version = layer.Version;
        if (layer.PrintConfig.HasValue) target.PrintConfig = layer.PrintConfig;
        if (!string.IsNullOrEmpty(layer.OutputDir)) target.OutputDir = layer.OutputDir;
        if (!string.IsNullOrEmpty(layer.SessionFile)) target.SessionFile = layer.SessionFile;
        if (!string.IsNullOrEmpty(layer.ConfigFile)) target.ConfigFile = layer.ConfigFile;
        if (!string.IsNullOrEmpty(layer.Aria2Path)) target.Aria2Path = layer.Aria2Path;
        if (!string.IsNullOrEmpty(layer.SpeedLimit)) target.SpeedLimit = layer.SpeedLimit;
        if (layer.Inputs is { Count: > 0 }) target.Inputs = layer.Inputs;
        if (layer.FromClipboard.HasValue) target.FromClipboard = layer.FromClipboard.Value;
        if (layer.FromFiles is { Count: > 0 }) target.FromFiles = layer.FromFiles;
        if (layer.ExtraOptions is { Count: > 0 }) target.ExtraOptions = layer.ExtraOptions;
    }

    private static AppSettings MergeOne(AppSettings baseline, SettingsOverlay overlay)
    {
        return new AppSettings
        {
            MaxConcurrentDownloads = overlay.MaxConcurrentDownloads ?? baseline.MaxConcurrentDownloads,
            Connections = overlay.Connections ?? baseline.Connections,
            Split = overlay.Split ?? baseline.Split,
            MaxConnectionPerServer = overlay.MaxConnectionPerServer ?? baseline.MaxConnectionPerServer,
            AutoFileRenaming = overlay.AutoFileRenaming ?? baseline.AutoFileRenaming,
            AllowOverwrite = overlay.AllowOverwrite ?? baseline.AllowOverwrite,
            SaveSession = overlay.SaveSession ?? baseline.SaveSession,
            SaveSessionInterval = overlay.SaveSessionInterval ?? baseline.SaveSessionInterval,
            RefreshIntervalMs = overlay.RefreshIntervalMs ?? baseline.RefreshIntervalMs,
            PollIntervalMs = overlay.PollIntervalMs ?? baseline.PollIntervalMs,
            Inline = overlay.Inline ?? baseline.Inline,
            Plain = overlay.Plain ?? baseline.Plain,
            NoColor = overlay.NoColor ?? baseline.NoColor,
            Ascii = overlay.Ascii ?? baseline.Ascii,
            NoPager = overlay.NoPager ?? baseline.NoPager,
            NoInstall = overlay.NoInstall ?? baseline.NoInstall,
            Verbose = overlay.Verbose ?? baseline.Verbose,
            Yes = overlay.Yes ?? baseline.Yes,
            Force = overlay.Force ?? baseline.Force,
            KeepListFiles = overlay.KeepListFiles ?? baseline.KeepListFiles,
            Resume = overlay.Resume ?? baseline.Resume,
            InstallDeps = overlay.InstallDeps ?? baseline.InstallDeps,
            InstallAria2 = overlay.InstallAria2 ?? baseline.InstallAria2,
            Help = overlay.Help ?? baseline.Help,
            Version = overlay.Version ?? baseline.Version,
            PrintConfig = overlay.PrintConfig ?? baseline.PrintConfig,
            OutputDir = overlay.OutputDir ?? baseline.OutputDir,
            SessionFile = overlay.SessionFile ?? baseline.SessionFile,
            ConfigFile = overlay.ConfigFile ?? baseline.ConfigFile,
            Aria2Path = overlay.Aria2Path ?? baseline.Aria2Path,
            SpeedLimit = overlay.SpeedLimit ?? baseline.SpeedLimit,
            Inputs = overlay.Inputs?.Count > 0 ? overlay.Inputs : baseline.Inputs,
            FromClipboard = overlay.FromClipboard ?? baseline.FromClipboard,
            FromFiles = overlay.FromFiles is { Count: > 0 } ? overlay.FromFiles : baseline.FromFiles,
            ExtraOptions = overlay.ExtraOptions?.Count > 0 ? overlay.ExtraOptions : baseline.ExtraOptions,
        };
    }
}

public interface ISettingsStore
{
    Task<SettingsOverlay> LoadOverlayAsync(CancellationToken ct);
    Task<SettingsOverlay> LoadOverlayAsync(string? path, CancellationToken ct);
    Task SaveOverlayAsync(SettingsOverlay overlay, CancellationToken ct);
    Task SaveOverlayAsync(SettingsOverlay overlay, string? path, CancellationToken ct);
}

public sealed class SettingsStore : ISettingsStore
{
    public Task<SettingsOverlay> LoadOverlayAsync(CancellationToken ct) => LoadOverlayAsync(null, ct);

    public async Task<SettingsOverlay> LoadOverlayAsync(string? path, CancellationToken ct)
    {
        path ??= AppPaths.SettingsFile;
        if (!File.Exists(path)) return new SettingsOverlay();
        try
        {
            var json = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
            return JsonSerializer.Deserialize<SettingsOverlay>(json, AppSettingsJsonContext.Default.SettingsOverlay) ?? new SettingsOverlay();
        }
        catch (JsonException ex)
        {
            await System.Console.Error.WriteLineAsync($"Warning: settings file corrupt ({ex.Message}); using defaults").ConfigureAwait(false);
            return new SettingsOverlay();
        }
    }

    public Task SaveOverlayAsync(SettingsOverlay overlay, CancellationToken ct) => SaveOverlayAsync(overlay, null, ct);

    public async Task SaveOverlayAsync(SettingsOverlay overlay, string? path, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(overlay);
        path ??= AppPaths.SettingsFile;
        var dir = Path.GetDirectoryName(path)!;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        var tmp = Path.Join(dir, $"settings-{Guid.NewGuid():N}.tmp");
        try
        {
            var json = JsonSerializer.Serialize(overlay, AppSettingsJsonContext.Default.SettingsOverlay);
            await File.WriteAllTextAsync(tmp, json, ct).ConfigureAwait(false);
            File.Move(tmp, path, overwrite: true);
        }
        catch
        {
            try { File.Delete(tmp); } catch (UnauthorizedAccessException) { }
            catch (IOException) { }
            throw;
        }
    }
}