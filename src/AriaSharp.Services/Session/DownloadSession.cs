using System.Net;


namespace AriaSharp.Services;

/// <summary>
/// Optional progress hook invoked after every poll. Lives in Services (no Spectre).
/// </summary>
public delegate void ProgressTick(SessionState state, TimeSpan elapsed);

public sealed class DownloadSession : IDownloadSession
{
    private readonly IAria2Engine _engine;
    private readonly IProgressCalculator _calculator;
    private readonly AppSettings _settings;
    private readonly ProgressTick? _onTick;
    private readonly Stopwatch _watch = new();
    private SessionState? _state;
    private int _stopRequested;

    public DownloadSession(
        IAria2Engine engine,
        IProgressCalculator calculator,
        AppSettings settings,
        ProgressTick? onTick = null)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _onTick = onTick;
    }

    public SessionState State => _state ?? EmptyState();

    public TimeSpan Elapsed => _watch.Elapsed;

    public async Task<RunOutcome> RunAsync(CancellationToken ct)
    {
        var pollMs = Math.Max(
            AppDefaults.PollFloorMs,
            Math.Max(AppDefaults.MinRefreshIntervalMs, _settings.RefreshIntervalMs));
        var idleRequired = AppDefaults.IdlePollsRequired;
        var idleCount = 0;
        var ticks = 0;
        long prevCompleted = 0, prevTotal = 0;
        double prevSpeed = 0;

        _watch.Start();
        try
        {
            while (!ct.IsCancellationRequested && Volatile.Read(ref _stopRequested) == 0)
            {
                EngineSnapshot snapshot;
                try
                {
                    snapshot = await _engine.GetSnapshotAsync(ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                ticks++;
                _state = _calculator.Compute(snapshot, prevCompleted, prevTotal, prevSpeed, ticks);
                prevCompleted = _state.CompletedLength;
                prevTotal = _state.TotalLength;
                prevSpeed = _state.TotalDownloadSpeed;

                _onTick?.Invoke(_state, _watch.Elapsed);

                if (_state is { NumActive: 0, NumWaiting: 0, NumPaused: 0 })
                {
                    idleCount++;
                    if (idleCount >= idleRequired)
                    {
                        await SettleAsync(ct);
                        return Classify(_state);
                    }
                }
                else
                {
                    idleCount = 0;
                }

                try
                {
                    await Task.Delay(pollMs, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        finally
        {
            _watch.Stop();
        }

        await SettleAsync(CancellationToken.None);
        return RunOutcome.Interrupted();
    }

    public Task RequestStopAsync()
    {
        Interlocked.Exchange(ref _stopRequested, 1);
        return Task.CompletedTask;
    }

    private static RunOutcome Classify(SessionState state)
    {
        var failed = state.FailedItems.Count;
        var complete = state.CompleteItems.Count;
        var total = state.AllItems.Count;

        if (total == 0)
            return RunOutcome.Empty("No downloads started — input had no usable URLs or aria2 rejected them.");

        if (failed > 0)
        {
            var first = state.FailedItems[0];
            var detail = first.ErrorMessage
                ?? (first.ErrorCode != 0 ? TextFor(first.ErrorCode) : "download failed");
            var msg = failed == 1
                ? detail
                : $"{failed} of {total} downloads failed. First: {detail}";
            return RunOutcome.Failed(msg);
        }

        if (complete == 0 && state.SkippedItems.Count == 0 && state.StoppedItems.Count == 0 && state.InterruptedItems.Count == 0)
            return RunOutcome.Empty("Nothing completed — check URLs and network.");

        return RunOutcome.Completed();
    }

    private async Task SettleAsync(CancellationToken ct)
    {
        try
        {
            await _engine.SaveSessionAsync(ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or OperationCanceledException or TimeoutException or IOException)
        {
            if (_settings.Verbose)
            {
                try { await System.Console.Error.WriteLineAsync($"warning: saveSession failed ({ex.Message})"); }
                catch (IOException) { }
            }
        }
    }

    private static SessionState EmptyState() => new()
    {
        ActiveItems = [],
        WaitingItems = [],
        StoppedItems = [],
        PausedItems = [],
        CompleteItems = [],
        FailedItems = [],
        SkippedItems = [],
        InterruptedItems = [],
        AllItems = [],
    };
}
