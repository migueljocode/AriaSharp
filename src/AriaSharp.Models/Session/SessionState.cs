namespace AriaSharp.Models;

public sealed class SessionState
{
    public long Timestamp { get; init; }
    public bool IsPaused { get; init; }
    public int NumActive { get; init; }
    public int NumWaiting { get; init; }
    public int NumStopped { get; init; }
    public int NumPaused { get; init; }
    public long TotalDownloadSpeed { get; init; }
    public long TotalUploadSpeed { get; init; }
    public long CompletedLength { get; init; }
    public long TotalLength { get; init; }
    public long ActiveCompleted { get; init; }
    public long ActiveTotal { get; init; }
    public long WaitingTotal { get; init; }
    public long StoppedTotal { get; init; }
    public long PausedTotal { get; init; }
    public long UnknownTotal { get; init; }
    public double OverallPercent { get; init; }
    public Confidence OverallConfidence { get; init; }
    public EtaEstimate OverallEta { get; init; } = EtaEstimate.Unknown;
    public required IReadOnlyList<DownloadItem> ActiveItems { get; init; }
    public required IReadOnlyList<DownloadItem> WaitingItems { get; init; }
    public required IReadOnlyList<DownloadItem> StoppedItems { get; init; }
    public required IReadOnlyList<DownloadItem> PausedItems { get; init; }
    public required IReadOnlyList<DownloadItem> CompleteItems { get; init; }
    public required IReadOnlyList<DownloadItem> FailedItems { get; init; }
    public required IReadOnlyList<DownloadItem> SkippedItems { get; init; }
    public required IReadOnlyList<DownloadItem> InterruptedItems { get; init; }
    public required IReadOnlyList<DownloadItem> AllItems { get; init; }
}