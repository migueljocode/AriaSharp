
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

        // Overall = every download (active + queued/waiting + paused + terminal)
        static long ItemTotal(DownloadItem i) =>
            i.TotalLength > 0 ? i.TotalLength : Math.Max(0, i.CompletedLength);
        static long ItemDone(DownloadItem i) => Math.Max(0, i.CompletedLength);

        var cohorts = new[]
        {
            activeItems, waitingItems, stoppedItems, pausedItems,
            completeItems, failedItems, skippedItems, interruptedItems,
        };

        long completed = 0, total = 0, unknownTotal = 0;
        foreach (var list in cohorts)
        {
            foreach (var i in list)
            {
                completed += ItemDone(i);
                var t = ItemTotal(i);
                total += t;
                if (i.TotalLength <= 0)
                    unknownTotal++;
            }
        }

        var activeCompleted = activeItems.Sum(ItemDone);
        var activeTotal = activeItems.Sum(ItemTotal);
        var waitingTotal = waitingItems.Sum(ItemTotal);
        var stoppedTotal = stoppedItems.Sum(ItemTotal);
        var pausedTotal = pausedItems.Sum(ItemTotal);

        Confidence confidence;
        double overallPercent;
        if (unknownTotal > 0 && total > 0)
        {
            confidence = Confidence.AtLeast;
            overallPercent = (double)completed / total * 100;
        }
        else if (total > 0)
        {
            confidence = Confidence.Exact;
            overallPercent = (double)completed / total * 100;
        }
        else
        {
            confidence = Confidence.Estimated;
            overallPercent = 0;
        }

        var speed = snapshot.TotalDownloadSpeed;
        var delta = completed - previousCompleted;
        if (speed <= 0 && delta > 0 && activeItems.Count > 0)
            speed = (long)(delta / (Math.Max(1, ticks) * 0.5));

        // Pause / idle: freeze ETA — do not keep EMA coasting
        var pausedOrIdle = activeItems.Count == 0 || speed <= 0;
        if (pausedOrIdle)
        {
            _emaSpeed = 0;
            _stableTicks = 0;
        }
        else
        {
            _emaSpeed = _emaSpeed == 0
                ? speed
                : (AppDefaults.EmaAlpha * speed + (1 - AppDefaults.EmaAlpha) * _emaSpeed);
            _stableTicks++;
        }

        var remaining = Math.Max(0, total - completed);
        EtaEstimate eta;
        if (!pausedOrIdle && _stableTicks >= AppDefaults.EtaStableTicks && _emaSpeed > 0 && remaining > 0)
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
            TotalDownloadSpeed = snapshot.TotalDownloadSpeed,
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
