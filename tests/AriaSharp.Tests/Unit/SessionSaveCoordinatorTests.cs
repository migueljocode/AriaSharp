namespace AriaSharp.Tests;

public class SessionSaveCoordinatorTests
{
    [Theory]
    [InlineData(EngineEventKind.Complete, true)]
    [InlineData(EngineEventKind.Error, true)]
    [InlineData(EngineEventKind.Stopped, true)]
    [InlineData(EngineEventKind.Started, false)]
    [InlineData(EngineEventKind.Paused, false)]
    [InlineData(EngineEventKind.Progress, false)]
    public void ShouldSaveSession_OnlyTerminalKinds(EngineEventKind kind, bool expected)
    {
        Assert.Equal(expected, SessionSaveCoordinator.ShouldSaveSession(kind));
    }

    [Fact]
    public async Task RequestSave_InvokesSaveOnce_WhenIdle()
    {
        var saves = 0;
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var coord = new SessionSaveCoordinator(async _ =>
        {
            Interlocked.Increment(ref saves);
            tcs.TrySetResult();
            await Task.CompletedTask;
        });

        coord.RequestSave();
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await Task.Delay(50);
        Assert.Equal(1, saves);
    }

    [Fact]
    public async Task RequestSave_WhileBusy_CoalescesToOneTrailingSave()
    {
        var saves = 0;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var coord = new SessionSaveCoordinator(async _ =>
        {
            var n = Interlocked.Increment(ref saves);
            if (n == 1)
            {
                entered.TrySetResult();
                await release.Task;
            }
            await Task.CompletedTask;
        });

        coord.RequestSave();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        coord.RequestSave();
        coord.RequestSave();
        release.TrySetResult();

        var deadline = DateTime.UtcNow.AddSeconds(3);
        while (Volatile.Read(ref saves) < 2 && DateTime.UtcNow < deadline)
            await Task.Delay(20);

        Assert.Equal(2, Volatile.Read(ref saves));
    }
}
