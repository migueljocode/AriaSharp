namespace AriaSharp.Models;

public sealed class EngineSnapshot
{
    public long Timestamp { get; init; }
    public required IReadOnlyList<DownloadItem> Active { get; init; }
    public required IReadOnlyList<DownloadItem> Waiting { get; init; }
    public required IReadOnlyList<DownloadItem> Stopped { get; init; }
    public required IReadOnlyList<DownloadItem> Paused { get; init; }
    public required IReadOnlyList<DownloadItem> Complete { get; init; }
    public required IReadOnlyList<DownloadItem> Failed { get; init; }
    public required IReadOnlyList<DownloadItem> Skipped { get; init; }
    public required IReadOnlyList<DownloadItem> Interrupted { get; init; }
    public int NumActive { get; init; }
    public int NumWaiting { get; init; }
    public long TotalDownloadSpeed { get; init; }
    public long TotalUploadSpeed { get; init; }
    public long CompletedLength { get; init; }
    public long TotalLength { get; init; }
}