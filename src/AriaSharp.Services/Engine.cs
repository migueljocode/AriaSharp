using System.Diagnostics;
using System.Threading.Channels;

using AriaSharp.Models;

namespace AriaSharp.Services;

public interface IAria2ArgsBuilder { string[] Build(EngineSettings settings, AppSettings appSettings); }

public sealed class Aria2ArgsBuilder : IAria2ArgsBuilder
{
    private static readonly string[] BlockedKeys =
    {
        "rpc-secret", "rpc-listen-port", "rpc-listen-all", "rpc-user", "rpc-pass",
        "enable-rpc", "conf-path", "no-conf", "stop-with-process", "input-file",
        "save-session", "save-session-interval"
    };
    public string[] Build(EngineSettings settings, AppSettings appSettings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(appSettings);
        var args = new List<string>
        {
            "--enable-rpc",
            $"--rpc-listen-port={settings.Port}",
            "--rpc-listen-all=false",
            $"--conf-path={settings.ConfFile}",
            $"--stop-with-process={Environment.ProcessId}",
            $"--input-file={settings.InputFile}",
            $"--dir={settings.OutputDir}",
            $"--max-concurrent-downloads={appSettings.MaxConcurrentDownloads}",
            $"--split={appSettings.Split}",
            $"--max-connection-per-server={appSettings.MaxConnectionPerServer}",
            $"--auto-file-renaming={Bool(appSettings.AutoFileRenaming)}",
            $"--allow-overwrite={Bool(appSettings.AllowOverwrite)}",
            "--summary-interval=0",
            "--show-console-readout=false",
            "--console-log-level=warn",
            "--enable-color=false",
            "--download-result=hide",
        };
        if (appSettings.SaveSession)
        {
            args.Add($"--save-session={settings.SessionFile}");
            args.Add($"--save-session-interval={appSettings.SaveSessionInterval}");
        }
        if (!string.IsNullOrEmpty(appSettings.SpeedLimit))
        {
            args.Add($"--max-overall-download-limit={appSettings.SpeedLimit}");
        }
        foreach (var pair in appSettings.ExtraOptions)
        {
            if (string.IsNullOrEmpty(pair)) continue;
            if (!TryParseOpt(pair, out var key, out var value)) continue;
            if (IsBlocked(key)) continue;
            args.Add($"--{key}={value}");
        }
        return args.ToArray();
    }

    private static string Bool(bool value) => value ? "true" : "false";

    private static bool TryParseOpt(string pair, out string key, out string value)
    {
        key = string.Empty; value = string.Empty;
        var idx = pair.IndexOf('=', StringComparison.Ordinal);
        if (idx <= 0 || idx == pair.Length - 1) return false;
        key = pair[..idx].Trim();
        value = pair[(idx + 1)..].Trim();
        return key.Length > 0;
    }

    private static bool IsBlocked(string key)
    {
        if (BlockedKeys.Contains(key, StringComparer.OrdinalIgnoreCase)) return true;
        return key.StartsWith("rpc-", StringComparison.OrdinalIgnoreCase)
            || key.StartsWith("on-", StringComparison.OrdinalIgnoreCase);
    }
}

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
    private readonly ChannelWriter<string> _log;
    public Aria2Process(string aria2Path, string[] args, ChannelWriter<string> log)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(log);
        _log = log;
        var psi = new ProcessStartInfo
        {
            FileName = aria2Path, UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true, RedirectStandardInput = true,
            WindowStyle = ProcessWindowStyle.Hidden,
        };
        foreach (var arg in args) psi.ArgumentList.Add(arg);
        _process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start aria2 process.");
        _process.OutputDataReceived += OnOutput; _process.ErrorDataReceived += OnError;
        _process.BeginOutputReadLine(); _process.BeginErrorReadLine();
    }
    private void OnOutput(object? sender, DataReceivedEventArgs e)
    {
        if (e.Data is { } data) _log.TryWrite(MaskCredentials(data));
    }
    private void OnError(object? sender, DataReceivedEventArgs e)
    {
        if (e.Data is { } data) _log.TryWrite(MaskCredentials(data));
    }
    public int Id => _process?.Id ?? -1;
    public bool HasExited => _process?.HasExited ?? true;
    public async Task WaitForExitAsync(CancellationToken ct) { if (_process == null) return; await _process.WaitForExitAsync(ct).ConfigureAwait(false); }
    public void Kill() { if (_process == null || _process.HasExited) return; try { _process.Kill(true); } catch (Exception) { } }
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

    private static string MaskCredentials(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        var span = value.AsSpan();
        var schemeEnd = span.IndexOf("://", StringComparison.Ordinal);
        if (schemeEnd < 0) return value;
        var at = span[(schemeEnd + 3)..].IndexOf('@');
        if (at < 0) return value;
        return value[..(schemeEnd + 3)] + "***@" + value[(schemeEnd + 4 + at)..];
    }
}

public sealed class LogWriter : IAsyncDisposable
{
    private readonly Channel<string> _channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true });
    private readonly StreamWriter _writer;
    private bool _disposed;
    public ChannelWriter<string> Writer => _channel.Writer;

    public LogWriter(string path)
    {
        var fs = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
        _writer = new StreamWriter(fs) { AutoFlush = true };
        _ = PumpAsync();
    }

    private async Task PumpAsync()
    {
        try
        {
            await foreach (var line in _channel.Reader.ReadAllAsync().ConfigureAwait(false))
                await _writer.WriteLineAsync(line).ConfigureAwait(false);
        }
        catch (Exception) { }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        _channel.Writer.Complete();
        await _writer.DisposeAsync().ConfigureAwait(false);
    }
}