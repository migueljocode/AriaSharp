using System.Diagnostics.CodeAnalysis;

namespace AriaSharp.Models;

public sealed class Aria2ErrorInfo
{
    public int Code { get; init; }
    public Aria2ErrorKind Kind { get; init; }
    public string Text { get; init; } = string.Empty;
    public string Hint { get; init; } = string.Empty;
    public string? Raw { get; init; }
    public static Aria2ErrorInfo None => new();
    public static Aria2ErrorInfo FromCode(int code, string? raw = null) => new()
    {
        Code = code, Kind = ErrorCatalog.Classify(code), Text = ErrorCatalog.TextFor(code), Hint = ErrorCatalog.HintFor(code), Raw = raw
    };
}

[SuppressMessage("Quality", "CA1062:Validate arguments of externally visible methods")]
public sealed class Aria2Exception : Exception
{
    public Aria2Exception(Aria2ErrorInfo info) : base(info?.Text ?? string.Empty) { Error = info ?? throw new ArgumentNullException(nameof(info)); }
    public Aria2Exception(string message) : base(message) { Error = Aria2ErrorInfo.FromCode(0, message); }
    public Aria2ErrorInfo Error { get; }
}

public sealed class EngineException : Exception
{
    public EngineException() { }
    public EngineException(string message) : base(message) { }
    public EngineException(string message, Exception inner) : base(message, inner) { }
}

public sealed class PrerequisiteException : Exception
{
    public PrerequisiteException() { }
    public PrerequisiteException(string message) : base(message) { }
    public PrerequisiteException(string message, Exception inner) : base(message, inner) { }
}