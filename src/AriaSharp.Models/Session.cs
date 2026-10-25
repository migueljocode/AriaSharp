namespace AriaSharp.Models;

public sealed class SessionState
{
    public long Timestamp { get; init; }
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
    public required DownloadItem[] ActiveItems { get; init; }
    public required DownloadItem[] WaitingItems { get; init; }
    public required DownloadItem[] StoppedItems { get; init; }
    public required DownloadItem[] PausedItems { get; init; }
    public required DownloadItem[] CompleteItems { get; init; }
    public required DownloadItem[] FailedItems { get; init; }
    public required DownloadItem[] SkippedItems { get; init; }
    public required DownloadItem[] InterruptedItems { get; init; }
    public required DownloadItem[] AllItems { get; init; }
}