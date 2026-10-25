namespace AriaSharp.Models;

public sealed class DownloadItem
{
    public string Gid { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DownloadState State { get; init; }
    public DownloadProtocol Protocol { get; init; } = DownloadProtocol.Unknown;
    public string EffectiveProtocol { get; init; } = string.Empty;
    public string Directory { get; init; } = string.Empty;
    public long CompletedLength { get; init; }
    public long TotalLength { get; init; }
    public long DownloadSpeed { get; init; }
    public int Connections { get; init; }
    public string? ErrorMessage { get; init; }
    public string? Uri { get; init; }
    public bool IsSelected { get; set; }
}

public sealed class DownloadSnapshot
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
}