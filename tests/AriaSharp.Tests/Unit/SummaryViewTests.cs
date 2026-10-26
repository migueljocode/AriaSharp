using AriaSharp.App;
using AriaSharp.Models;
using Spectre.Console;
using Spectre.Console.Rendering;
using Xunit;

namespace AriaSharp.Tests;

public class SummaryViewTests
{
    [Fact]
    public void Build_Failed_ShowsRetryPromptWhenRequested()
    {
        var state = new SessionState
        {
            ActiveItems = [], WaitingItems = [], StoppedItems = [], PausedItems = [],
            CompleteItems = [],
            FailedItems =
            [
                new DownloadItem
                {
                    Gid = "g1", Name = "bad.bin", State = DownloadState.Failed,
                    ErrorCode = 3, ErrorMessage = "Resource not found",
                },
            ],
            SkippedItems = [], InterruptedItems = [], AllItems = [],
        };
        var outcome = RunOutcome.Failed("Resource not found");
        var renderable = SummaryView.Build(state, outcome, TimeSpan.FromSeconds(2), "/tmp/out", null, showRetryPrompt: true);
        var text = Render(renderable, 80);
        Assert.Contains("FAILED", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("retry", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Render_Plain_ContainsResult()
    {
        var state = new SessionState
        {
            ActiveItems = [], WaitingItems = [], StoppedItems = [], PausedItems = [],
            CompleteItems = [], FailedItems = [], SkippedItems = [], InterruptedItems = [], AllItems = [],
            CompletedLength = 100, TotalLength = 100,
        };
        var text = SummaryView.Render(state, RunOutcome.Completed(), TimeSpan.FromSeconds(1), "/tmp", null);
        Assert.Contains("ok", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Summary", text, StringComparison.Ordinal);
    }

    private static string Render(IRenderable r, int width)
    {
        var sw = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.Yes,
            ColorSystem = ColorSystemSupport.TrueColor,
            Out = new AnsiConsoleOutput(sw),
            Interactive = InteractionSupport.No,
        });
        console.Profile.Width = width;
        console.Write(r);
        return sw.ToString();
    }
}
