using System.Diagnostics;

using Spectre.Console;

using AriaSharp.Models;

namespace AriaSharp.App;

/// <summary>
/// Renders the dashboard at a fixed animation rate, decoupled from the aria2 poll:
/// the session publishes snapshots, this loop glides the bar between them and drives shimmer, spinner and intro.
/// It is the only writer to the live display and owns the alternate-screen guard.
/// </summary>
internal sealed class DashboardLoop(LiveDisplayContext ctx, int split)
{
    private sealed record Latest(SessionState State, TimeSpan Elapsed, long At);

    private volatile Latest? _latest;

    public ScreenGuard? Guard { get; private set; }

    public void Publish(SessionState state, TimeSpan elapsed) => _latest = new Latest(state, elapsed, Stopwatch.GetTimestamp());

    public async Task RunAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(AppDefaults.AnimationFrameMs));
        var shown = 0.0;
        var last = Stopwatch.GetTimestamp();
        var frame = 0;
        var intro = -1;
        try
        {
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                if (_latest is not { } latest) continue;
                var s = latest.State;
                var hasWork = s.AllItems.Count > 0 || s.NumActive > 0 || s.NumWaiting > 0 || s.NumPaused > 0
                    || s.FailedItems.Count > 0 || s.CompleteItems.Count > 0 || s.SkippedItems.Count > 0;
                if (hasWork && Guard is null)
                    Guard = ScreenGuard.TryEnter();
                if (Guard is null && !hasWork) continue;
                if (intro < 0) intro = frame;

                var now = Stopwatch.GetTimestamp();
                shown = Gradient.Glide(shown, s.OverallPercent / 100.0, Stopwatch.GetElapsedTime(last, now).TotalSeconds);
                last = now;
                var elapsed = latest.Elapsed + Stopwatch.GetElapsedTime(latest.At, now);
                ctx.UpdateTarget(DashboardView.Build(s, elapsed, split, frame - intro, shown));
                frame++;
            }
        }
        catch (OperationCanceledException) { }
        catch (InvalidOperationException) { }
        catch (IOException) { }
    }
}
