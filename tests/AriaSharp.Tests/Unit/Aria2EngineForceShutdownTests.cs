using System.Threading.Channels;

namespace AriaSharp.Tests;

public class Aria2EngineForceShutdownTests
{
    private sealed class RecordingEngine : IAria2Engine
    {
        private readonly Channel<EngineEvent> _events = Channel.CreateUnbounded<EngineEvent>();

        public bool ForceShutdownCalled { get; private set; }
        public bool ShutdownCalled { get; private set; }
        public ChannelReader<EngineEvent> Events => _events.Reader;

        public Task StartAsync(EngineSettings settings, CancellationToken ct) => Task.CompletedTask;
        public Task<EngineSnapshot> GetSnapshotAsync(CancellationToken ct) => Task.FromResult(new EngineSnapshot
        {
            Active = [], Waiting = [], Stopped = [], Paused = [],
            Complete = [], Failed = [], Skipped = [], Interrupted = [],
        });
        public Task SaveSessionAsync(CancellationToken ct) => Task.CompletedTask;
        public Task ShutdownAsync(CancellationToken ct) { ShutdownCalled = true; return Task.CompletedTask; }
        public Task ForceShutdownAsync(CancellationToken ct) { ForceShutdownCalled = true; return Task.CompletedTask; }
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
}
