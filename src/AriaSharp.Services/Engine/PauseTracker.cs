namespace AriaSharp.Services;

/// <summary>
/// Remembers which downloads were active when the user paused. aria2 pauses the whole queue,
/// so this is the only way to tell "paused" apart from "queued" in the display.
/// </summary>
internal sealed class PauseTracker
{
    private readonly HashSet<string> _pausedGids = new(StringComparer.Ordinal);

    public bool IsPaused => _pausedGids.Count > 0;

    public void Record(IEnumerable<string> activeGids)
    {
        _pausedGids.Clear();
        foreach (var gid in activeGids)
            _pausedGids.Add(gid);
    }

    public void Clear() => _pausedGids.Clear();

    /// <summary>Items aria2 reports as paused count as Paused only if they were active at pause time; the rest are Waiting.</summary>
    public (List<DownloadItem> Waiting, List<DownloadItem> Paused) Split(IReadOnlyList<DownloadItem> items)
    {
        var waiting = new List<DownloadItem>();
        var paused = new List<DownloadItem>();
        foreach (var item in items)
        {
            if (item.State == DownloadState.Paused && _pausedGids.Contains(item.Gid))
                paused.Add(item);
            else
                waiting.Add(item);
        }
        return (waiting, paused);
    }
}
