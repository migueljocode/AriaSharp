namespace AriaSharp.Models;

public sealed class EngineEvent
{
    public EngineEventKind Kind { get; init; }
    public string? Gid { get; init; }
    public string? Message { get; init; }
    public string? Detail { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
