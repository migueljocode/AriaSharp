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
        Code = code, Kind = ErrorCatalog.Classify(code), Text = TextFor(code), Hint = HintFor(code), Raw = raw
    };
}

public sealed class Aria2Exception : Exception
{
    public Aria2Exception() : this(Aria2ErrorInfo.None) { }
    public Aria2Exception(string message) : this(Aria2ErrorInfo.FromCode(0, message)) { }
    public Aria2Exception(string message, Exception inner) : this(Aria2ErrorInfo.FromCode(0, message), inner) { }
    public Aria2Exception(Aria2ErrorInfo info) : this(info, null) { }
    private Aria2Exception(Aria2ErrorInfo info, Exception? inner) : base(info?.Text ?? string.Empty, inner)
    {
        Error = info ?? throw new ArgumentNullException(nameof(info));
    }
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

public sealed class CliException : Exception
{
    public CliException() { }
    public CliException(string message) : base(message) { }
    public CliException(string message, Exception inner) : base(message, inner) { }
}