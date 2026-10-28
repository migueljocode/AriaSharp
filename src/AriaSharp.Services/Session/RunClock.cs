namespace AriaSharp.Services;

/// <summary>Elapsed-time clock that only runs while downloads are not paused.</summary>
internal sealed class RunClock
{
    private readonly Stopwatch _watch = new();

    public TimeSpan Elapsed => _watch.Elapsed;

    /// <summary>Stops the clock while paused and runs it otherwise. Both calls are no-ops when already in that state.</summary>
    public void Sync(bool paused)
    {
        if (paused)
            _watch.Stop();
        else
            _watch.Start();
    }

    public void Stop() => _watch.Stop();
}
