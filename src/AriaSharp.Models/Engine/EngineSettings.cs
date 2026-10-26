namespace AriaSharp.Models;

public sealed class EngineSettings
{
    public required string Aria2Path { get; init; }
    public required string OutputDir { get; init; }
    public required string SessionFile { get; init; }
    public required string InputFile { get; init; }
    public required string ConfFile { get; init; }
    public required string Secret { get; init; }
    public required int Port { get; init; }
    public required IReadOnlyList<string> Args { get; init; }
}
