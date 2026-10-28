
namespace AriaSharp.Tests;

public class PauseTrackerTests
{
    private static readonly string[] ExpectedWaiting = ["a", "d"];
    private static readonly string[] ExpectedPaused = ["b", "c"];

    [Fact]
    public void Split_OnlyItemsActiveAtPauseCountAsPaused()
    {
        var tracker = new PauseTracker();
        tracker.Record(["b", "c"]);
        DownloadItem[] queue =
        [
            new() { Gid = "a", State = DownloadState.Paused },
            new() { Gid = "b", State = DownloadState.Paused },
            new() { Gid = "c", State = DownloadState.Paused },
            new() { Gid = "d", State = DownloadState.Paused },
        ];

        var (waiting, paused) = tracker.Split(queue);

        Assert.Equal(ExpectedWaiting, waiting.Select(i => i.Gid));
        Assert.Equal(ExpectedPaused, paused.Select(i => i.Gid));
    }

    [Fact]
    public void Clear_OnResume_ReturnsEverythingToWaiting()
    {
        var tracker = new PauseTracker();
        tracker.Record(["b"]);
        tracker.Clear();

        var (waiting, paused) = tracker.Split([new DownloadItem { Gid = "b", State = DownloadState.Paused }]);

        Assert.Single(waiting);
        Assert.Empty(paused);
        Assert.False(tracker.IsPaused);
    }
}
