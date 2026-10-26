using AriaSharp.Models;
using AriaSharp.Services;
using Xunit;

namespace AriaSharp.Tests;

public class ProgressCalculatorTests
{
    private static DownloadItem Item(long completed, long total, DownloadState state = DownloadState.Active) =>
        new()
        {
            Gid = Guid.NewGuid().ToString("N")[..8],
            CompletedLength = completed,
            TotalLength = total,
            State = state,
            Status = state.ToString(),
        };

    private static EngineSnapshot Snapshot(
        IReadOnlyList<DownloadItem>? active = null,
        IReadOnlyList<DownloadItem>? waiting = null,
        IReadOnlyList<DownloadItem>? complete = null,
        IReadOnlyList<DownloadItem>? paused = null,
        long speed = 0) =>
        new()
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Active = active ?? [],
            Waiting = waiting ?? [],
            Stopped = [],
            Paused = paused ?? [],
            Complete = complete ?? [],
            Failed = [],
            Skipped = [],
            Interrupted = [],
            TotalDownloadSpeed = speed,
        };

    [Fact]
    public void Compute_IncludesCompleteItemsInTotals()
    {
        var calc = new ProgressCalculator();
        var snap = Snapshot(
            active: [Item(50, 100)],
            complete: [Item(200, 200, DownloadState.Complete)]);

        var state = calc.Compute(snap, previousCompleted: 0, previousTotal: 0, previousSpeed: 0, ticks: 1);

        Assert.Equal(250, state.CompletedLength);
        Assert.Equal(300, state.TotalLength);
        Assert.Single(state.CompleteItems);
        Assert.True(state.OverallPercent > 80);
    }

    [Fact]
    public void Compute_ActiveOnly_MatchesActiveTotals()
    {
        var calc = new ProgressCalculator();
        var snap = Snapshot(active: [Item(40, 100), Item(10, 50)], speed: 1024);

        var state = calc.Compute(snap, 0, 0, 0, 1);

        Assert.Equal(50, state.CompletedLength);
        Assert.Equal(150, state.TotalLength);
        Assert.Equal(2, state.NumActive);
        Assert.Equal(1024, state.TotalDownloadSpeed);
        Assert.Equal(Confidence.Exact, state.OverallConfidence);
    }

    [Fact]
    public void Compute_UnknownSize_UsesAtLeastConfidence()
    {
        var calc = new ProgressCalculator();
        var snap = Snapshot(active: [Item(10, 0), Item(20, 100)]);

        var state = calc.Compute(snap, 0, 0, 0, 1);

        Assert.Equal(Confidence.AtLeast, state.OverallConfidence);
        Assert.Equal(1, state.UnknownTotal);
    }

    [Fact]
    public void Compute_IncludesWaitingInOverall()
    {
        var calc = new ProgressCalculator();
        var snap = Snapshot(
            active: [Item(50, 100)],
            waiting: [Item(0, 400)]);

        var state = calc.Compute(snap, 0, 0, 0, 1);

        Assert.Equal(50, state.CompletedLength);
        Assert.Equal(500, state.TotalLength);
        Assert.Equal(1, state.NumWaiting);
        Assert.True(state.OverallPercent < 15);
    }

    [Fact]
    public void Compute_Paused_FreezesEta()
    {
        var calc = new ProgressCalculator();
        // warm up EMA
        var active = Snapshot(active: [Item(50, 100)], speed: 10_000);
        _ = calc.Compute(active, 0, 0, 0, 5);
        _ = calc.Compute(active, 50, 100, 10_000, 6);
        // all paused
        var paused = Snapshot(paused: [Item(50, 100, DownloadState.Paused)], speed: 0);
        var state = calc.Compute(paused, 50, 100, 10_000, 7);
        Assert.Equal(Confidence.Unknown, state.OverallEta.Confidence);
        Assert.Null(state.OverallEta.Seconds);
    }

    [Fact]
    public void Compute_EmptySnapshot_EstimatedZero()
    {
        var calc = new ProgressCalculator();
        var state = calc.Compute(Snapshot(), 0, 0, 0, 1);

        Assert.Equal(0, state.CompletedLength);
        Assert.Equal(0, state.TotalLength);
        Assert.Equal(Confidence.Estimated, state.OverallConfidence);
        Assert.Equal(Confidence.Unknown, state.OverallEta.Confidence);
        Assert.Null(state.OverallEta.Seconds);
    }
}
