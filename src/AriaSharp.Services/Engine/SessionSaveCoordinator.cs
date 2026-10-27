namespace AriaSharp.Services;

/// <summary>
/// Single-flight session save with trailing flush when terminal events arrive during an in-flight save.
/// <see cref="RequestSave"/> returns immediately (safe to call from the WebSocket receive loop).
/// </summary>
internal sealed class SessionSaveCoordinator
{
    private readonly Func<CancellationToken, Task> _saveAsync;
    private int _busy;
    private int _dirty;

    public SessionSaveCoordinator(Func<CancellationToken, Task> saveAsync)
    {
        _saveAsync = saveAsync ?? throw new ArgumentNullException(nameof(saveAsync));
    }

    /// <summary>True for terminal download outcomes that should persist resume.session.</summary>
    public static bool ShouldSaveSession(EngineEventKind kind) =>
        kind is EngineEventKind.Complete or EngineEventKind.Error or EngineEventKind.Stopped;

    public void RequestSave()
    {
        Interlocked.Exchange(ref _dirty, 1);
        _ = RunLoopAsync();
    }

    /// <summary>Request a save and wait until idle (end-of-run settle).</summary>
    public async Task FlushAsync(CancellationToken ct)
    {
        RequestSave();
        while (Volatile.Read(ref _busy) == 1 || Volatile.Read(ref _dirty) == 1)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(15, ct);
        }
    }

    private async Task RunLoopAsync()
    {
        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
            return;

        try
        {
            while (Interlocked.Exchange(ref _dirty, 0) == 1)
            {
                try
                {
                    await _saveAsync(CancellationToken.None);
                }
                catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException
                    or OperationCanceledException or TimeoutException or IOException)
                {
                    // Best-effort.
                }
            }
        }
        finally
        {
            Interlocked.Exchange(ref _busy, 0);
            if (Volatile.Read(ref _dirty) == 1)
                _ = RunLoopAsync();
        }
    }
}
