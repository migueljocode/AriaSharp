namespace AriaSharp.Models;

public enum RunOutcomeKind { Completed, Interrupted, Failed, Empty }

public sealed class RunOutcome
{
    private RunOutcome(RunOutcomeKind kind, string? message = null)
    {
        Kind = kind;
        Message = message;
    }

    public RunOutcomeKind Kind { get; }
    public string? Message { get; }

    public static RunOutcome Completed(string? message = null) => new(RunOutcomeKind.Completed, message);
    public static RunOutcome Interrupted(string? message = null) => new(RunOutcomeKind.Interrupted, message);
    public static RunOutcome Failed(string? message = null) => new(RunOutcomeKind.Failed, message);
    public static RunOutcome Empty(string? message = null) => new(RunOutcomeKind.Empty, message);

    public bool IsSuccess => Kind is RunOutcomeKind.Completed or RunOutcomeKind.Empty;
}
