using AriaSharp.Models;

namespace AriaSharp.App;

/// <summary>
/// Limits progress line spam when stdout/stderr is redirected (CI, logs).
/// Always allows the first tick and any change in complete/failed counts.
/// </summary>
internal sealed class ProgressThrottle
{
    private readonly int _minIntervalMs;
    private long _lastEmitMs = -1;
    private int _lastComplete = -1;
    private int _lastFailed = -1;

    public ProgressThrottle(int minIntervalMs = 1000) =>
        _minIntervalMs = Math.Max(100, minIntervalMs);

    public bool ShouldEmit(SessionState state, TimeSpan elapsed)
    {
        ArgumentNullException.ThrowIfNull(state);
        var complete = state.CompleteItems.Count;
        var failed = state.FailedItems.Count;
        var now = (long)elapsed.TotalMilliseconds;

        if (_lastEmitMs < 0)
        {
            Record(now, complete, failed);
            return true;
        }

        if (complete != _lastComplete || failed != _lastFailed)
        {
            Record(now, complete, failed);
            return true;
        }

        if (now - _lastEmitMs >= _minIntervalMs)
        {
            Record(now, complete, failed);
            return true;
        }

        return false;
    }

    private void Record(long now, int complete, int failed)
    {
        _lastEmitMs = now;
        _lastComplete = complete;
        _lastFailed = failed;
    }
}
