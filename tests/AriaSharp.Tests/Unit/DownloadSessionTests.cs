using System.Threading.Channels;

using AriaSharp.Models;
using AriaSharp.Services;
using Xunit;

namespace AriaSharp.Tests;

public class DownloadSessionTests
{
    [Fact]
    public async Task RunAsync_IdleWithComplete_ReturnsCompleted()
    {
        var engine = new ScriptedEngine(
        [
            Snapshot(complete: [Item("a", DownloadState.Complete, 100, 100)]),
            Snapshot(complete: [Item("a", DownloadState.Complete, 100, 100)]),
        ]);
        try
        {
            var session = new DownloadSession(engine, new ProgressCalculator(), new AppSettings { RefreshIntervalMs = 100 });
            var outcome = await session.RunAsync(CancellationToken.None).ConfigureAwait(true);
            Assert.Equal(RunOutcomeKind.Completed, outcome.Kind);
        }
        finally
        {
            await engine.DisposeAsync().ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task RunAsync_IdleWithFailed_ReturnsFailed()
    {
        var engine = new ScriptedEngine(
        [
            Snapshot(failed: [Item("b", DownloadState.Failed, 0, 0, errorCode: 3, error: "Resource not found")]),
            Snapshot(failed: [Item("b", DownloadState.Failed, 0, 0, errorCode: 3, error: "Resource not found")]),
        ]);
        try
        {
            var session = new DownloadSession(engine, new ProgressCalculator(), new AppSettings { RefreshIntervalMs = 100 });
            var outcome = await session.RunAsync(CancellationToken.None).ConfigureAwait(true);
            Assert.Equal(RunOutcomeKind.Failed, outcome.Kind);
            Assert.Contains("Resource not found", outcome.Message, StringComparison.Ordinal);
        }
        finally
        {
            await engine.DisposeAsync().ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task RunAsync_NoItems_ReturnsEmpty()
    {
        var engine = new ScriptedEngine([Snapshot(), Snapshot()]);
        try
        {
            var session = new DownloadSession(engine, new ProgressCalculator(), new AppSettings { RefreshIntervalMs = 100 });
            var outcome = await session.RunAsync(CancellationToken.None).ConfigureAwait(true);
            Assert.Equal(RunOutcomeKind.Empty, outcome.Kind);
        }
        finally
        {
            await engine.DisposeAsync().ConfigureAwait(true);
        }
    }

    private static DownloadItem Item(
        string gid, DownloadState state, long completed, long total,
        int errorCode = 0, string? error = null) => new()
    {
        Gid = gid,
        Name = gid,
        Status = state.ToString(),
        State = state,
        CompletedLength = completed,
        TotalLength = total,
        ErrorCode = errorCode,
        ErrorMessage = error,
    };

    private static EngineSnapshot Snapshot(
        IReadOnlyList<DownloadItem>? active = null,
        IReadOnlyList<DownloadItem>? complete = null,
        IReadOnlyList<DownloadItem>? failed = null) => new()
    {
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        Active = active ?? [],
        Waiting = [],
        Stopped = [],
        Paused = [],
        Complete = complete ?? [],
        Failed = failed ?? [],
        Skipped = [],
        Interrupted = [],
    };

    private sealed class ScriptedEngine : IAria2Engine
    {
        private readonly Queue<EngineSnapshot> _script;
        private readonly Channel<EngineEvent> _events = Channel.CreateUnbounded<EngineEvent>();

        public ScriptedEngine(IEnumerable<EngineSnapshot> script) => _script = new Queue<EngineSnapshot>(script);

        public ChannelReader<EngineEvent> Events => _events.Reader;
        public Task StartAsync(EngineSettings settings, CancellationToken ct) => Task.CompletedTask;
        public Task SaveSessionAsync(CancellationToken ct) => Task.CompletedTask;
        public Task ShutdownAsync(CancellationToken ct) => Task.CompletedTask;
        public Task<long> AddUriAsync(string[] uris, CancellationToken ct) => Task.FromResult(0L);
        public Task<string> GetVersionAsync(CancellationToken ct) => Task.FromResult("1.37.0");
        public ValueTask DisposeAsync() { _events.Writer.TryComplete(); return ValueTask.CompletedTask; }

        public Task<EngineSnapshot> GetSnapshotAsync(CancellationToken ct)
        {
            if (_script.Count == 0)
                return Task.FromResult(Snapshot());
            return Task.FromResult(_script.Dequeue());
        }
    }
}
