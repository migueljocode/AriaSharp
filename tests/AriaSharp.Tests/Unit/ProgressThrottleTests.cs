
namespace AriaSharp.Tests;

public class ProgressThrottleTests
{
    private static SessionState State(int complete = 0, int failed = 0) => new()
    {
        ActiveItems = [],
        WaitingItems = [],
        StoppedItems = [],
        PausedItems = [],
        CompleteItems = Enumerable.Range(0, complete).Select(i => new DownloadItem { Gid = $"c{i}" }).ToList(),
        FailedItems = Enumerable.Range(0, failed).Select(i => new DownloadItem { Gid = $"f{i}" }).ToList(),
        SkippedItems = [],
        InterruptedItems = [],
        AllItems = [],
    };

    [Fact]
    public void FirstTick_AlwaysEmits()
    {
        var t = new ProgressThrottle(1000);
        Assert.True(t.ShouldEmit(State(), TimeSpan.FromMilliseconds(0)));
    }

    [Fact]
    public void WithinInterval_Suppresses()
    {
        var t = new ProgressThrottle(1000);
        Assert.True(t.ShouldEmit(State(), TimeSpan.FromMilliseconds(0)));
        Assert.False(t.ShouldEmit(State(), TimeSpan.FromMilliseconds(100)));
        Assert.False(t.ShouldEmit(State(), TimeSpan.FromMilliseconds(500)));
    }

    [Fact]
    public void AfterInterval_Emits()
    {
        var t = new ProgressThrottle(1000);
        Assert.True(t.ShouldEmit(State(), TimeSpan.FromMilliseconds(0)));
        Assert.True(t.ShouldEmit(State(), TimeSpan.FromMilliseconds(1000)));
    }

    [Fact]
    public void CompleteCountChange_EmitsImmediately()
    {
        var t = new ProgressThrottle(1000);
        Assert.True(t.ShouldEmit(State(complete: 0), TimeSpan.FromMilliseconds(0)));
        Assert.True(t.ShouldEmit(State(complete: 1), TimeSpan.FromMilliseconds(50)));
    }

    [Fact]
    public void FailedCountChange_EmitsImmediately()
    {
        var t = new ProgressThrottle(1000);
        Assert.True(t.ShouldEmit(State(failed: 0), TimeSpan.FromMilliseconds(0)));
        Assert.True(t.ShouldEmit(State(failed: 1), TimeSpan.FromMilliseconds(10)));
    }
}
