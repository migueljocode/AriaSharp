using AriaSharp.Services;
using Xunit;

namespace AriaSharp.Tests;

public class Aria2EngineSnapshotTests
{
    private static readonly string[] ExpectedWaiting = ["a"];
    private static readonly string[] ExpectedPaused = ["b", "c"];

    [Fact]
    public void SplitPaused_MovesPausedOutOfWaiting()
    {
        DownloadItem[] queue =
        [
            new() { Gid = "a", State = DownloadState.Waiting },
            new() { Gid = "b", State = DownloadState.Paused },
            new() { Gid = "c", State = DownloadState.Paused },
        ];

        var (waiting, paused) = Aria2Engine.SplitPaused([.. queue]);

        Assert.Equal(ExpectedWaiting, waiting.Select(i => i.Gid));
        Assert.Equal(ExpectedPaused, paused.Select(i => i.Gid));
    }
}
