namespace AriaSharp.Models;

public enum RunOutcomeKind { Completed, Interrupted, Failed, Empty }

public sealed class RunOutcome
{
    private RunOutcome(RunOutcomeKind kind, string? message = null) { Kind = kind; Message = message; }
    public RunOutcomeKind Kind { get; }
    public string? Message { get; }
    public static RunOutcome Completed(string? m = null) => new(RunOutcomeKind.Completed, m);
    public static RunOutcome Interrupted(string? m = null) => new(RunOutcomeKind.Interrupted, m);
    public static RunOutcome Failed(string? m = null) => new(RunOutcomeKind.Failed, m);
    public static RunOutcome Empty(string? m = null) => new(RunOutcomeKind.Empty, m);
}