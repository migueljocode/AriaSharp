using System.IO;

namespace AriaSharp.Services;

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
            await foreach (var line in _channel.Reader.ReadAllAsync())
            {
                await _writer.WriteLineAsync(line);
            }
        }
        catch (OperationCanceledException) { }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        _channel.Writer.Complete();
        await _writer.DisposeAsync();
    }
}
