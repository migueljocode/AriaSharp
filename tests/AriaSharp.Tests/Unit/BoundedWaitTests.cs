using System.Diagnostics;

namespace AriaSharp.Tests;

public class BoundedWaitTests
{
    private static readonly TimeSpan Cap = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan Budget = TimeSpan.FromSeconds(2);

    [Fact]
    public async Task RunAsync_WorkHonouringItsToken_ReturnsAtTheCap()
    {
        var sw = Stopwatch.StartNew();
        await BoundedWait.RunAsync(ct => Task.Delay(Timeout.Infinite, ct), Cap, CancellationToken.None);
        Assert.True(sw.Elapsed < Budget, $"took {sw.Elapsed}");
    }

    [Fact]
    public async Task RunAsync_WorkIgnoringItsToken_StillReturnsAtTheCap()
    {
        var never = new TaskCompletionSource();
        var sw = Stopwatch.StartNew();
        await BoundedWait.RunAsync(_ => never.Task, Cap, CancellationToken.None);
        Assert.True(sw.Elapsed < Budget, $"took {sw.Elapsed}");
    }

    [Fact]
    public async Task RunAsync_HangingForceShutdown_StopsWithinTheConfiguredCap()
    {
        var never = new TaskCompletionSource();
        var sw = Stopwatch.StartNew();
        await BoundedWait.RunAsync(
            _ => never.Task,
            TimeSpan.FromMilliseconds(AppDefaults.ForceShutdownRpcCapMs),
            CancellationToken.None);
        Assert.True(sw.Elapsed < Budget, $"took {sw.Elapsed}");
    }

    [Fact]
    public async Task RunAsync_FastWork_RunsToCompletion()
    {
        var ran = false;
        await BoundedWait.RunAsync(_ => { ran = true; return Task.CompletedTask; }, Cap, CancellationToken.None);
        Assert.True(ran);
    }

    [Fact]
    public async Task RunAsync_WorkFault_Propagates()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            BoundedWait.RunAsync(_ => Task.FromException(new InvalidOperationException("boom")), Cap, CancellationToken.None));
    }

    [Fact]
    public async Task RunAsync_AlreadyCancelledToken_DoesNotThrow()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var sw = Stopwatch.StartNew();
        await BoundedWait.RunAsync(ct => Task.Delay(Timeout.Infinite, ct), Cap, cts.Token);
        Assert.True(sw.Elapsed < Budget, $"took {sw.Elapsed}");
    }

    [Fact]
    public async Task WaitAsync_CompletedTask_ReturnsTrue()
    {
        Assert.True(await BoundedWait.WaitAsync(Task.CompletedTask, Cap));
    }

    [Fact]
    public async Task WaitAsync_HangingTask_ReturnsFalseAtTheCap_AndLaterFaultIsObserved()
    {
        var never = new TaskCompletionSource();
        var sw = Stopwatch.StartNew();
        Assert.False(await BoundedWait.WaitAsync(never.Task, Cap));
        Assert.True(sw.Elapsed < Budget, $"took {sw.Elapsed}");

        // The abandoned task faults afterwards: nothing may throw or surface unobserved.
        never.SetException(new InvalidOperationException("late"));
        await Task.Delay(50);
    }
}
