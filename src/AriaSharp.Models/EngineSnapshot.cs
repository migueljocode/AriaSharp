namespace AriaSharp.Models;

public sealed class EngineSnapshot
{
    public long Timestamp { get; init; }
    public required DownloadItem[] Active { get; init; }
    public required DownloadItem[] Waiting { get; init; }
    public required DownloadItem[] Stopped { get; init; }
    public required DownloadItem[] Paused { get; init; }
    public required DownloadItem[] Complete { get; init; }
    public required DownloadItem[] Failed { get; init; }
    public required DownloadItem[] Skipped { get; init; }
    public required DownloadItem[] Interrupted { get; init; }
    public int NumActive { get; init; }
    public int NumWaiting { get; init; }
    public long TotalDownloadSpeed { get; init; }
    public long TotalUploadSpeed { get; init; }
    public long CompletedLength { get; init; }
    public long TotalLength { get; init; }
}