using System.Net;

using AriaSharp.Models;

namespace AriaSharp.Services;

public interface IDownloadSession
{
    SessionState State { get; }
    Task<RunOutcome> RunAsync(CancellationToken ct);
    Task RequestStopAsync();
}

public sealed class DownloadSession : IDownloadSession
{
    private readonly IAria2Engine _engine;
    private readonly IProgressCalculator _calculator;
    private readonly AppSettings _settings;
    private SessionState? _state;
    public DownloadSession(IAria2Engine engine, IProgressCalculator calculator, AppSettings settings)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }
    public SessionState State => _state ?? new SessionState { ActiveItems = Array.Empty<DownloadItem>(), WaitingItems = Array.Empty<DownloadItem>(), StoppedItems = Array.Empty<DownloadItem>(), PausedItems = Array.Empty<DownloadItem>(), CompleteItems = Array.Empty<DownloadItem>(), FailedItems = Array.Empty<DownloadItem>(), SkippedItems = Array.Empty<DownloadItem>(), InterruptedItems = Array.Empty<DownloadItem>(), AllItems = Array.Empty<DownloadItem>() };
    public async Task<RunOutcome> RunAsync(CancellationToken ct)
    {
        var pollMs = Math.Max(AppDefaults.PollFloorMs, Math.Max(AppDefaults.MinRefreshIntervalMs, _settings.RefreshIntervalMs));
        var idleRequired = AppDefaults.IdlePollsRequired;
        var idleCount = 0; var ticks = 0; long prevCompleted = 0, prevTotal = 0; double prevSpeed = 0;
        while (!ct.IsCancellationRequested)
        {
            var snapshot = await _engine.GetSnapshotAsync(ct).ConfigureAwait(false);
            ticks++;
            _state = _calculator.Compute(snapshot, prevCompleted, prevTotal, prevSpeed, ticks);
            prevCompleted = _state.CompletedLength; prevTotal = _state.TotalLength; prevSpeed = _state.TotalDownloadSpeed;
            if (_state.NumActive == 0 && _state.NumWaiting == 0)
            {
                idleCount++;
                if (idleCount >= idleRequired) { await SettleAsync(ct).ConfigureAwait(false); return RunOutcome.Completed(); }
            }
            else idleCount = 0;
            try { await Task.Delay(pollMs, ct).ConfigureAwait(false); } catch (OperationCanceledException) { break; }
        }
        await SettleAsync(ct).ConfigureAwait(false);
        return RunOutcome.Interrupted();
    }
    private async Task SettleAsync(CancellationToken ct) { try { await _engine.SaveSessionAsync(ct).ConfigureAwait(false); } catch (HttpRequestException) { } catch (InvalidOperationException) { } }
    public Task RequestStopAsync() => Task.CompletedTask;
}