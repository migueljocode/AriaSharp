namespace AriaSharp.Tests;

public class DashboardGuardTests
{
    private static readonly DownloadItem Item = new() { Gid = "0000000000000001", State = DownloadState.Complete };

    private static EngineSnapshot Snapshot(
        DownloadItem[]? paused = null,
        DownloadItem[]? stopped = null,
        DownloadItem[]? complete = null,
        DownloadItem[]? failed = null,
        DownloadItem[]? skipped = null,
        DownloadItem[]? interrupted = null,
        int numActive = 0,
        int numWaiting = 0,
        long totalLength = 0)
        => new()
        {
            Active = [],
            Waiting = [],
            Paused = paused ?? [],
            Stopped = stopped ?? [],
            Complete = complete ?? [],
            Failed = failed ?? [],
            Skipped = skipped ?? [],
            Interrupted = interrupted ?? [],
            NumActive = numActive,
            NumWaiting = numWaiting,
            TotalLength = totalLength,
        };

    [Fact]
    public void NothingAtAll_IsEmptyStart() => Assert.True(DashboardRunner.IsEmptyStart(Snapshot()));

    [Fact]
    public void ActiveOrWaitingOrSized_IsNotEmptyStart()
    {
        Assert.False(DashboardRunner.IsEmptyStart(Snapshot(numActive: 1)));
        Assert.False(DashboardRunner.IsEmptyStart(Snapshot(numWaiting: 1)));
        Assert.False(DashboardRunner.IsEmptyStart(Snapshot(totalLength: 10)));
    }

    [Fact]
    public void InstantlyFinishedDownload_IsNotEmptyStart()
        => Assert.False(DashboardRunner.IsEmptyStart(Snapshot(complete: [Item])));

    [Fact]
    public void InstantlyFailedDownload_IsNotEmptyStart()
        => Assert.False(DashboardRunner.IsEmptyStart(Snapshot(failed: [Item])));

    [Fact]
    public void SkippedOrInterruptedOrStoppedOrPaused_IsNotEmptyStart()
    {
        Assert.False(DashboardRunner.IsEmptyStart(Snapshot(skipped: [Item])));
        Assert.False(DashboardRunner.IsEmptyStart(Snapshot(interrupted: [Item])));
        Assert.False(DashboardRunner.IsEmptyStart(Snapshot(stopped: [Item])));
        Assert.False(DashboardRunner.IsEmptyStart(Snapshot(paused: [Item])));
    }
}
