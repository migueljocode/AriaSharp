using System.Threading.Channels;


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
    public async Task RunAsync_AllPaused_KeepsRunningUntilCancelled()
    {
        var engine = new ScriptedEngine(Enumerable.Repeat(Snapshot(paused: [Item("a", DownloadState.Paused, 10, 100)]), 50));
        try
        {
            var session = new DownloadSession(engine, new ProgressCalculator(), new AppSettings { RefreshIntervalMs = 100 });
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(600));
            var outcome = await session.RunAsync(cts.Token).ConfigureAwait(true);
            Assert.Equal(RunOutcomeKind.Interrupted, outcome.Kind);
        }
        finally
        {
            await engine.DisposeAsync().ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task RunAsync_AllSkipped_ReturnsCompleted()
    {
        var engine = new ScriptedEngine(Enumerable.Repeat(Snapshot(skipped: [Item("a", DownloadState.Skipped, 0, 0)]), 3));
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
    public async Task RunAsync_CancelledDuringSnapshot_ReturnsInterrupted()
    {
        using var cts = new CancellationTokenSource();
        var engine = new ScriptedEngine(Enumerable.Repeat(Snapshot(active: [Item("a", DownloadState.Active, 1, 100)]), 5))
        {
            OnSnapshot = cts.Cancel,
        };
        try
        {
            var session = new DownloadSession(engine, new ProgressCalculator(), new AppSettings { RefreshIntervalMs = 100 });
            var outcome = await session.RunAsync(cts.Token).ConfigureAwait(true);
            Assert.Equal(RunOutcomeKind.Interrupted, outcome.Kind);
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
        IReadOnlyList<DownloadItem>? paused = null,
        IReadOnlyList<DownloadItem>? skipped = null,
        IReadOnlyList<DownloadItem>? complete = null,
        IReadOnlyList<DownloadItem>? failed = null) => new()
    {
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        Active = active ?? [],
        Waiting = [],
        Stopped = [],
        Paused = paused ?? [],
        Complete = complete ?? [],
        Failed = failed ?? [],
        Skipped = skipped ?? [],
        Interrupted = [],
    };

    private sealed class ScriptedEngine : IAria2Engine
    {
        private readonly Queue<EngineSnapshot> _script;
        private readonly Channel<EngineEvent> _events = Channel.CreateUnbounded<EngineEvent>();

        public ScriptedEngine(IEnumerable<EngineSnapshot> script) => _script = new Queue<EngineSnapshot>(script);

        public Action? OnSnapshot { get; init; }
        public ChannelReader<EngineEvent> Events => _events.Reader;
        public Task StartAsync(EngineSettings settings, CancellationToken ct) => Task.CompletedTask;
        public Task SaveSessionAsync(CancellationToken ct) => Task.CompletedTask;
        public Task ShutdownAsync(CancellationToken ct) => Task.CompletedTask;
        public Task<string> AddUriAsync(string[] uris, CancellationToken ct) => Task.FromResult(string.Empty);
        public Task<string> GetVersionAsync(CancellationToken ct) => Task.FromResult("1.37.0");
        public ValueTask DisposeAsync() { _events.Writer.TryComplete(); return ValueTask.CompletedTask; }

        public Task<EngineSnapshot> GetSnapshotAsync(CancellationToken ct)
        {
            OnSnapshot?.Invoke();
            ct.ThrowIfCancellationRequested();
            if (_script.Count == 0)
                return Task.FromResult(Snapshot());
            return Task.FromResult(_script.Dequeue());
        }
    }
}
