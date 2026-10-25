using System.Diagnostics;
using System.Text;
using AriaSharp.Models;

namespace AriaSharp.Services;

public interface IAria2ArgsBuilder { string[] Build(EngineSettings settings, AppSettings appSettings); }

public sealed class Aria2ArgsBuilder : IAria2ArgsBuilder
{
    public string[] Build(EngineSettings settings, AppSettings appSettings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(appSettings);
        var args = new List<string>
        {
            "--enable-rpc=true",
            $"--rpc-listen-port={settings.Port}",
            "--rpc-listen-all=false",
            $"--rpc-secret={settings.Secret}",
            $"--conf-path={settings.TempConfFile}",
            $"--stop-with-process={Environment.ProcessId}",
            $"--input-file={settings.TempInputFile}",
            $"--dir={settings.OutputDir}",
            $"--max-concurrent-downloads={appSettings.MaxConcurrentDownloads}",
            $"--split={appSettings.Split}",
            $"--max-connection-per-server={appSettings.MaxConnectionPerServer}",
            "--auto-file-renaming=" + appSettings.AutoFileRenaming.ToString().ToUpperInvariant(),
            "--allow-overwrite=" + appSettings.AllowOverwrite.ToString().ToUpperInvariant(),
            $"--save-session={settings.SessionFile}",
            $"--save-session-interval={appSettings.SaveSessionInterval}",
            "--summary-interval=0",
            "--show-console-readout=false",
            "--console-log-level=warn",
            "--enable-color=false",
            "--download-result=hide",
        };
        if (!string.IsNullOrEmpty(appSettings.SpeedLimit)) args.Add($"--max-overall-download-limit={appSettings.SpeedLimit}");
        if (!string.IsNullOrEmpty(appSettings.Aria2Opt))
            foreach (var pair in appSettings.Aria2Opt.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                if (pair.Contains('=', StringComparison.Ordinal)) args.Add($"--aria2-opt {pair}");
        foreach (var opt in appSettings.ExtraOptions)
            if (!string.IsNullOrEmpty(opt)) args.Add($"--aria2-opt {opt}");
        return args.ToArray();
    }
}

public interface IAria2Locator { Task<string> LocateAsync(CancellationToken ct); }

public sealed class Aria2Locator : IAria2Locator
{
    private readonly string? _explicitPath;
    public Aria2Locator(string? explicitPath = null) { _explicitPath = explicitPath; }
    public Task<string> LocateAsync(CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_explicitPath))
        {
            if (File.Exists(_explicitPath)) return Task.FromResult(_explicitPath!);
            throw new FileNotFoundException("aria2 not found at the explicit path.", _explicitPath);
        }
        var candidates = new[] { "aria2c", "aria2", "/usr/bin/aria2c", "/usr/local/bin/aria2c", "/opt/homebrew/bin/aria2c", "/usr/local/aria2/bin/aria2c" };
        foreach (var c in candidates)
        {
            var path = FindInPath(c);
            if (!string.IsNullOrEmpty(path) && File.Exists(path)) return Task.FromResult(path!);
        }
        throw new FileNotFoundException("aria2c was not found in PATH or known locations. Use --aria2-path or run the installer.");
    }
    private static string? FindInPath(string command)
    {
        var pathVar = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var extensions = Environment.OSVersion.Platform == PlatformID.Win32NT ? new[] { ".exe", ".cmd", ".bat" } : new[] { string.Empty };
        foreach (var dir in pathVar.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            foreach (var ext in extensions)
            {
                var full = Path.Combine(dir, command + ext);
                if (File.Exists(full)) return full;
            }
        return null;
    }
}

public sealed class Aria2Process : IAsyncDisposable
{
    private Process? _process;
    private readonly StringBuilder _output = new();
    public Aria2Process(string aria2Path, string[] args, string logPath)
    {
        ArgumentNullException.ThrowIfNull(args);
        var psi = new ProcessStartInfo
        {
            FileName = aria2Path, UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true, RedirectStandardInput = true,
            WindowStyle = ProcessWindowStyle.Hidden,
        };
        foreach (var arg in args) psi.ArgumentList.Add(arg);
        _process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start aria2 process.");
        _process.OutputDataReceived += OnData; _process.ErrorDataReceived += OnData;
        _process.BeginOutputReadLine(); _process.BeginErrorReadLine();
    }
    private void OnData(object? sender, DataReceivedEventArgs e) { if (e.Data != null) lock (_output) _output.AppendLine(e.Data); }
    public int Id => _process?.Id ?? -1;
    public bool HasExited => _process?.HasExited ?? true;
    public async Task WaitForExitAsync(CancellationToken ct) { if (_process == null) return; await _process.WaitForExitAsync(ct).ConfigureAwait(false); }
    public void Kill() { if (_process == null || _process.HasExited) return; try { _process.Kill(true); } catch (Exception) { } }
    public string ReadOutput() { lock (_output) return _output.ToString(); }
    public async ValueTask DisposeAsync()
    {
        if (_process == null) return;
        try
        {
            if (!_process.HasExited) { _process.Kill(true); using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3)); await _process.WaitForExitAsync(cts.Token).ConfigureAwait(false); }
        }
        catch (Exception) { }
        finally { _process.Dispose(); _process = null; }
    }
}