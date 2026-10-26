using AriaSharp.Services;
using Xunit;

namespace AriaSharp.Tests;

public class Aria2EngineSnapshotTests
{
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

        Assert.Equal(new[] { "a" }, waiting.Select(i => i.Gid));
        Assert.Equal(new[] { "b", "c" }, paused.Select(i => i.Gid));
    }
}
