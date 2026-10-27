namespace AriaSharp.Models;

/// <summary>C# 14 extension members for run outcomes.</summary>
public static class RunOutcomeExtensions
{
    extension(RunOutcome outcome)
    {
        public int ToExitCode() => (int)(outcome.Kind switch
        {
            RunOutcomeKind.Completed => ExitCode.Ok,
            RunOutcomeKind.Interrupted => ExitCode.Interrupted,
            _ => ExitCode.Failure,
        });
    }
}
