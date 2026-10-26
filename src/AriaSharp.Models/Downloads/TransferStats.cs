namespace AriaSharp.Models;

public sealed class TransferStats
{
    public long Completed { get; init; }
    public long Total { get; init; }
    public long Remaining { get; init; }
    public double Percent { get; init; }
    public long Speed { get; init; }
    public double SmoothedSpeed { get; init; }
    public double EmaSpeed { get; init; }
    public double? EtaSeconds { get; init; }
    public EtaEstimate Eta { get; init; } = EtaEstimate.Unknown;
    public DateTime? StartTime { get; init; }
    public DateTime? EndTime { get; init; }
    public long Connections { get; init; }
}