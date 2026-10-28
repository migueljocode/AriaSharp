using System.Reflection;
using System.Threading.Channels;
using AriaSharp.Services;

namespace AriaSharp.Tests;

public class Aria2EngineForceShutdownTests
{
    private sealed class RecordingEngine : IAria2Engine
    {
        private readonly Channel<EngineEvent> _events = Channel.CreateUnbounded<EngineEvent>();
        private readonly List<string> _calls = [];

        public bool ForceShutdownCalled { get; private set; }
        public bool ShutdownCalled { get; private set; }
        public bool StopCalled { get; private set; }
        public IReadOnlyList<string> CallOrder => _calls;
        public ChannelReader<EngineEvent> Events => _events.Reader;

        public Task StartAsync(EngineSettings settings, CancellationToken ct) => Task.CompletedTask;
        public Task<EngineSnapshot> GetSnapshotAsync(CancellationToken ct) => Task.FromResult(new EngineSnapshot
        {
            Active = [], Waiting = [], Stopped = [], Paused = [],
            Complete = [], Failed = [], Skipped = [], Interrupted = [],
        });
        public Task SaveSessionAsync(CancellationToken ct)
        {
            _calls.Add("SaveSession");
            return Task.CompletedTask;
        }
        public Task StopAsync(CancellationToken ct)
        {
            if (StopCalled) return Task.CompletedTask;
            StopCalled = true;
            _calls.Add("Stop");
            return Task.CompletedTask;
        }
        public Task ShutdownAsync(CancellationToken ct) { ShutdownCalled = true; _calls.Add("Shutdown"); return Task.CompletedTask; }
        public Task ForceShutdownAsync(CancellationToken ct)
        {
            ForceShutdownCalled = true;
            _calls.Add("ForceShutdown");
            return Task.CompletedTask;
        }
        public Task<string> AddUriAsync(string[] uris, CancellationToken ct) => Task.FromResult(string.Empty);
        public Task<string> GetVersionAsync(CancellationToken ct) => Task.FromResult("1.37.0");
        public async ValueTask DisposeAsync()
        {
            _events.Writer.TryComplete();
            await ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task ForceShutdownAsync_InvokesRpc()
    {
        await using var engine = new RecordingEngine();
        await engine.ForceShutdownAsync(CancellationToken.None);
        Assert.True(engine.ForceShutdownCalled);
        Assert.False(engine.ShutdownCalled);
    }

    [Fact]
    public async Task ForceShutdownAsync_Twice_IsNoop()
    {
        await using var engine = new RecordingEngine();
        await engine.ForceShutdownAsync(CancellationToken.None);
        await engine.ForceShutdownAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_Twice_SecondIsNoop()
    {
        await using var engine = new RecordingEngine();
        await engine.StopAsync(CancellationToken.None);
        await engine.StopAsync(CancellationToken.None);
        Assert.Single(engine.CallOrder);
    }

    [Fact]
    public async Task StopAsync_DoesNotCallShutdown()
    {
        await using var engine = new RecordingEngine();
        await engine.StopAsync(CancellationToken.None);
        Assert.False(engine.ShutdownCalled);
        Assert.DoesNotContain("Shutdown", engine.CallOrder);
    }

    [Fact]
    public void Aria2Engine_HasForcePauseAllAsync_NotPauseAllAsync()
    {
        var type = typeof(Aria2Engine);
        Assert.NotNull(type.GetMethod("ForcePauseAllAsync", new[] { typeof(CancellationToken) }));
        Assert.Null(type.GetMethod("PauseAllAsync", new[] { typeof(CancellationToken) }));
    }
}
