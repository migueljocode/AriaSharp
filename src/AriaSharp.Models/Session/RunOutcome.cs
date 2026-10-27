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

    /// <summary>Non-fatal warnings surfaced during the run (e.g. saveSession failures).
    /// The Console layer prints these only when Verbose, and only after the screen is restored.</summary>
    public IList<string> Warnings { get; } = [];

    public static RunOutcome Completed(string? message = null) => new(RunOutcomeKind.Completed, message);
    public static RunOutcome Interrupted(string? message = null) => new(RunOutcomeKind.Interrupted, message);
    public static RunOutcome Failed(string? message = null) => new(RunOutcomeKind.Failed, message);
    public static RunOutcome Empty(string? message = null) => new(RunOutcomeKind.Empty, message);

    public bool IsSuccess => Kind is RunOutcomeKind.Completed or RunOutcomeKind.Empty;
}
