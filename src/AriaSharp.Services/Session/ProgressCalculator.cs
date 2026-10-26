using AriaSharp.Models;

namespace AriaSharp.Services;

public interface IProgressCalculator
{
    SessionState Compute(EngineSnapshot snapshot, long previousCompleted, long previousTotal, double previousSpeed, int ticks);
}

public sealed class ProgressCalculator : IProgressCalculator
{
    private double _emaSpeed;
    private int _stableTicks;

    public SessionState Compute(EngineSnapshot snapshot, long previousCompleted, long previousTotal, double previousSpeed, int ticks)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var activeItems = snapshot.Active;
        var waitingItems = snapshot.Waiting;
        var stoppedItems = snapshot.Stopped;
        var pausedItems = snapshot.Paused;
        var completeItems = snapshot.Complete;
        var failedItems = snapshot.Failed;
        var skippedItems = snapshot.Skipped;
        var interruptedItems = snapshot.Interrupted;

        var activeCompleted = activeItems.Sum(i => i.CompletedLength);
        var activeTotal = activeItems.Sum(i => i.TotalLength);
        var waitingTotal = waitingItems.Sum(i => i.TotalLength);
        var stoppedTotal = stoppedItems.Sum(i => i.TotalLength);
        var pausedTotal = pausedItems.Sum(i => i.TotalLength);
        var completeTotal = completeItems.Sum(i => i.TotalLength > 0 ? i.TotalLength : i.CompletedLength);
        var completeCompleted = completeItems.Sum(i => i.CompletedLength > 0 ? i.CompletedLength : i.TotalLength);

        // Failed / skipped / interrupted contribute their completed length but not open work.
        var terminalCompleted = completeCompleted
            + failedItems.Sum(i => i.CompletedLength)
            + skippedItems.Sum(i => i.CompletedLength)
            + interruptedItems.Sum(i => i.CompletedLength);

        var unknownTotal = activeItems.Concat(waitingItems).Concat(stoppedItems).Concat(pausedItems)
            .Sum(i => i.TotalLength <= 0 ? 1L : 0L);

        var totalKnown = activeTotal + waitingTotal + stoppedTotal + pausedTotal + completeTotal;
        var completed = activeCompleted
            + stoppedItems.Sum(i => i.CompletedLength)
            + pausedItems.Sum(i => i.CompletedLength)
            + terminalCompleted;

        var total = totalKnown;
        Confidence confidence;
        double overallPercent;
        if (unknownTotal > 0 && totalKnown > 0)
        {
            confidence = Confidence.AtLeast;
            overallPercent = total > 0 ? (double)completed / total * 100 : 0;
        }
        else if (totalKnown > 0)
        {
            confidence = Confidence.Exact;
            overallPercent = (double)completed / total * 100;
        }
        else
        {
            confidence = Confidence.Estimated;
            overallPercent = 0;
        }

        var delta = completed - previousCompleted;
        var speed = snapshot.TotalDownloadSpeed;
        if (speed <= 0 && delta > 0)
            speed = (long)(delta / (Math.Max(1, ticks) * 0.5));

        _emaSpeed = _emaSpeed == 0
            ? speed
            : (AppDefaults.EmaAlpha * speed + (1 - AppDefaults.EmaAlpha) * _emaSpeed);

        if (speed > 0) _stableTicks++;
        else _stableTicks = 0;

        var remaining = Math.Max(0, total - completed);
        EtaEstimate eta;
        if (_stableTicks >= AppDefaults.EtaStableTicks && _emaSpeed > 0)
        {
            var seconds = remaining / _emaSpeed;
            eta = confidence == Confidence.Exact ? EtaEstimate.Exact(seconds) : EtaEstimate.AtLeast(seconds);
        }
        else
        {
            eta = EtaEstimate.Unknown;
        }

        var allItems = new List<DownloadItem>(
            activeItems.Count + waitingItems.Count + stoppedItems.Count + pausedItems.Count
            + completeItems.Count + failedItems.Count + skippedItems.Count + interruptedItems.Count);
        allItems.AddRange(activeItems);
        allItems.AddRange(waitingItems);
        allItems.AddRange(stoppedItems);
        allItems.AddRange(pausedItems);
        allItems.AddRange(completeItems);
        allItems.AddRange(failedItems);
        allItems.AddRange(skippedItems);
        allItems.AddRange(interruptedItems);

        return new SessionState
        {
            Timestamp = snapshot.Timestamp,
            NumActive = activeItems.Count,
            NumWaiting = waitingItems.Count,
            NumStopped = stoppedItems.Count,
            NumPaused = pausedItems.Count,
            TotalDownloadSpeed = speed,
            TotalUploadSpeed = snapshot.TotalUploadSpeed,
            CompletedLength = completed,
            TotalLength = total,
            ActiveCompleted = activeCompleted,
            ActiveTotal = activeTotal,
            WaitingTotal = waitingTotal,
            StoppedTotal = stoppedTotal,
            PausedTotal = pausedTotal,
            UnknownTotal = unknownTotal,
            OverallPercent = overallPercent,
            OverallConfidence = confidence,
            OverallEta = eta,
            ActiveItems = activeItems,
            WaitingItems = waitingItems,
            StoppedItems = stoppedItems,
            PausedItems = pausedItems,
            CompleteItems = completeItems,
            FailedItems = failedItems,
            SkippedItems = skippedItems,
            InterruptedItems = interruptedItems,
            AllItems = allItems,
        };
    }
}