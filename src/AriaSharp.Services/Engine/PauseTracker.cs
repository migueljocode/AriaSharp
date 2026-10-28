namespace AriaSharp.Services;

/// <summary>
/// Remembers which downloads were active when the user paused. aria2 pauses the whole queue,
/// so this is the only way to tell "paused" apart from "queued" in the display.
/// Written by the key handler and read by the poll loop, hence the lock.
/// </summary>
internal sealed class PauseTracker
{
    private readonly Lock _gate = new();
    private readonly HashSet<string> _pausedGids = new(StringComparer.Ordinal);
    private bool _paused;

    public bool IsPaused
    {
        get
        {
            lock (_gate)
                return _paused;
        }
    }

    /// <summary>Marks the session paused and adds the gids that were active. Additive, so pressing pause again keeps the first set; pausing with nothing active still counts as paused.</summary>
    public void Record(IEnumerable<string> activeGids)
    {
        lock (_gate)
        {
            _paused = true;
            foreach (var gid in activeGids)
                _pausedGids.Add(gid);
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _paused = false;
            _pausedGids.Clear();
        }
    }

    /// <summary>Items aria2 reports as paused count as Paused only if they were active at pause time; the rest are Waiting.</summary>
    public (List<DownloadItem> Waiting, List<DownloadItem> Paused) Split(IEnumerable<DownloadItem> items)
    {
        var waiting = new List<DownloadItem>();
        var paused = new List<DownloadItem>();
        lock (_gate)
        {
            foreach (var item in items)
            {
                if (item.State == DownloadState.Paused && _pausedGids.Contains(item.Gid))
                    paused.Add(item);
                else
                    waiting.Add(item);
            }
        }
        return (waiting, paused);
    }
}
