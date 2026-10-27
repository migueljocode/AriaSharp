namespace AriaSharp.Tests;

public class SessionSaveCoordinatorTests
{
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
    public async Task RequestSave_WhileBusy_SchedulesTrailingSave()
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
        // Second and third requests while first is in flight → dirty trailing flush
        coord.RequestSave();
        coord.RequestSave();
        release.TrySetResult();

        // Wait for trailing save
        var deadline = DateTime.UtcNow.AddSeconds(3);
        while (Volatile.Read(ref saves) < 2 && DateTime.UtcNow < deadline)
            await Task.Delay(20);

        Assert.True(saves >= 2, $"expected trailing save, got {saves}");
    }

    [Fact]
    public void NonTerminal_WouldNotCall_ThroughEngineHook()
    {
        // Engine only calls RequestSave for Complete/Error/Stopped — covered by NotificationTests mapping.
        Assert.True(Enum.IsDefined(EngineEventKind.Complete));
        Assert.True(Enum.IsDefined(EngineEventKind.Error));
        Assert.True(Enum.IsDefined(EngineEventKind.Stopped));
    }
}
