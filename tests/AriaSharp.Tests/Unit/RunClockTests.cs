
namespace AriaSharp.Tests;

public class RunClockTests
{
    [Fact]
    public void Sync_Paused_FreezesElapsed()
    {
        var clock = new RunClock();
        clock.Sync(paused: false);
        Thread.Sleep(30);
        clock.Sync(paused: true);
        var frozen = clock.Elapsed;

        Thread.Sleep(60);

        Assert.Equal(frozen, clock.Elapsed);
        Assert.True(frozen > TimeSpan.Zero);
    }

    [Fact]
    public void Sync_Resumed_ContinuesFromFrozenValue()
    {
        var clock = new RunClock();
        clock.Sync(paused: false);
        Thread.Sleep(20);
        clock.Sync(paused: true);
        var frozen = clock.Elapsed;

        clock.Sync(paused: false);
        Thread.Sleep(30);

        Assert.True(clock.Elapsed > frozen);
    }

    [Fact]
    public void Sync_RepeatedSameState_IsHarmless()
    {
        var clock = new RunClock();
        clock.Sync(paused: false);
        clock.Sync(paused: false);
        clock.Sync(paused: true);
        clock.Sync(paused: true);

        Assert.True(clock.Elapsed >= TimeSpan.Zero);
    }
}
