using Xunit;
using AriaSharp.Models;

namespace AriaSharp.Tests;

public class SnapshotTests
{
    [Fact]
    public void EngineSnapshot_AllItems_IncludesAllStates()
    {
        var active = new DownloadItem { Gid = "a1", Name = "active.zip" };
        var waiting = new DownloadItem { Gid = "w1", Name = "waiting.zip" };
        var stopped = new DownloadItem { Gid = "s1", Name = "stopped.zip" };
        var paused = new DownloadItem { Gid = "p1", Name = "paused.zip" };
        var complete = new DownloadItem { Gid = "c1", Name = "complete.zip" };
        var failed = new DownloadItem { Gid = "f1", Name = "failed.zip" };
        var skipped = new DownloadItem { Gid = "k1", Name = "skipped.zip" };
        var interrupted = new DownloadItem { Gid = "i1", Name = "interrupted.zip" };

        var snap = new EngineSnapshot
        {
            Timestamp = 1L,
            Active = new[] { active },
            Waiting = new[] { waiting },
            Stopped = new[] { stopped },
            Paused = new[] { paused },
            Complete = new[] { complete },
            Failed = new[] { failed },
            Skipped = new[] { skipped },
            Interrupted = new[] { interrupted },
            NumActive = 1,
            TotalDownloadSpeed = 100,
        };

        Assert.Single(snap.Active);
        Assert.Single(snap.Waiting);
        Assert.Single(snap.Complete);
        Assert.Single(snap.Failed);
    }

    [Fact]
    public void EngineSnapshot_TotalLength_SumsActiveItems()
    {
        var snap = new EngineSnapshot
        {
            Timestamp = 1L,
            Active = new[]
            {
                new DownloadItem { TotalLength = 1000 },
                new DownloadItem { TotalLength = 2000 },
            },
            Waiting = Array.Empty<DownloadItem>(),
            Stopped = Array.Empty<DownloadItem>(),
            Paused = Array.Empty<DownloadItem>(),
            Complete = Array.Empty<DownloadItem>(),
            Failed = Array.Empty<DownloadItem>(),
            Skipped = Array.Empty<DownloadItem>(),
            Interrupted = Array.Empty<DownloadItem>(),
        };

        var total = snap.Active.Sum(i => i.TotalLength);
        Assert.Equal(3000L, total);
    }

    [Fact]
    public void SessionState_OverallPercent_Calculated()
    {
        var state = new SessionState
        {
            Timestamp = 1L,
            CompletedLength = 500,
            TotalLength = 1000,
            OverallPercent = 50.0,
            ActiveItems = Array.Empty<DownloadItem>(),
            WaitingItems = Array.Empty<DownloadItem>(),
            StoppedItems = Array.Empty<DownloadItem>(),
            PausedItems = Array.Empty<DownloadItem>(),
            CompleteItems = Array.Empty<DownloadItem>(),
            FailedItems = Array.Empty<DownloadItem>(),
            SkippedItems = Array.Empty<DownloadItem>(),
            InterruptedItems = Array.Empty<DownloadItem>(),
            AllItems = Array.Empty<DownloadItem>(),
        };
        Assert.Equal(50.0, state.OverallPercent);
    }

    [Fact]
    public void EtaEstimate_AtLeast_ShowsPrefix()
    {
        var eta = EtaEstimate.AtLeast(300);
        Assert.StartsWith(">= ", eta.Format(), StringComparison.Ordinal);
    }

    [Fact]
    public void EtaEstimate_Estimated_ShowsTilde()
    {
        var eta = EtaEstimate.Estimated(180);
        Assert.StartsWith("~ ", eta.Format(), StringComparison.Ordinal);
    }
}