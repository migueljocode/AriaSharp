using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Channels;

namespace AriaSharp.Services;

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
            FileName = aria2Path,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = false,
            WindowStyle = ProcessWindowStyle.Hidden,
        };
        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }
        _process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start aria2 process.");
        _process.OutputDataReceived += OnOutput;
        _process.ErrorDataReceived += OnError;
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
    }

    private void OnOutput(object? sender, DataReceivedEventArgs e)
    {
        if (e.Data is { } data)
        {
            _log.TryWrite(MaskCredentials(data));
        }
    }

    private void OnError(object? sender, DataReceivedEventArgs e)
    {
        if (e.Data is { } data)
        {
            _log.TryWrite(MaskCredentials(data));
        }
    }

    public int Id => _process?.Id ?? -1;

    public bool HasExited => _process?.HasExited ?? true;

    public Task WaitForExitAsync(CancellationToken ct)
    {
        if (_process == null)
        {
            return Task.CompletedTask;
        }
        return _process.WaitForExitAsync(ct);
    }

    public void Kill()
    {
        if (_process == null || _process.HasExited)
        {
            return;
        }
        try
        {
            _process.Kill(true);
        }
        catch (InvalidOperationException) { }
        catch (Win32Exception) { }
    }

    public async ValueTask DisposeAsync()
    {
        if (_process == null)
        {
            return;
        }
        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(true);
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                await _process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
            }
        }
        catch (InvalidOperationException) { }
        catch (Win32Exception) { }
        finally
        {
            _process.Dispose();
            _process = null;
        }
    }

    private static string MaskCredentials(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }
        var span = value.AsSpan();
        var schemeEnd = span.IndexOf("://", StringComparison.Ordinal);
        if (schemeEnd < 0)
        {
            return value;
        }
        var at = span[(schemeEnd + 3)..].IndexOf('@');
        if (at < 0)
        {
            return value;
        }
        return value[..(schemeEnd + 3)] + "***@" + value[(schemeEnd + 4 + at)..];
    }
}
