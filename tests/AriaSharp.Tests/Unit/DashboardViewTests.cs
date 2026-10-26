using AriaSharp.App;
using AriaSharp.Models;
using Spectre.Console;
using Spectre.Console.Rendering;
using Xunit;

namespace AriaSharp.Tests;

public class DashboardViewTests
{
    [Fact]
    public void Build_EmptyState_ReturnsRenderable()
    {
        var state = new SessionState
        {
            ActiveItems = [],
            WaitingItems = [],
            StoppedItems = [],
            PausedItems = [],
            CompleteItems = [],
            FailedItems = [],
            SkippedItems = [],
            InterruptedItems = [],
            AllItems = [],
        };
        var renderable = DashboardView.Build(state, TimeSpan.FromSeconds(1));
        Assert.NotNull(renderable);
        // Smoke: measure without throwing
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.Yes,
            ColorSystem = ColorSystemSupport.TrueColor,
            Out = new AnsiConsoleOutput(new StringWriter()),
            Interactive = InteractionSupport.No,
        });
        console.Write(renderable);
    }

    [Fact]
    public void Build_WithActiveAndFailed_Renders()
    {
        var state = new SessionState
        {
            NumActive = 1,
            OverallPercent = 50,
            TotalDownloadSpeed = 2048,
            CompletedLength = 500,
            TotalLength = 1000,
            ActiveItems =
            [
                new DownloadItem
                {
                    Gid = "g1",
                    Name = "bigfile.zip",
                    CompletedLength = 500,
                    TotalLength = 1000,
                    DownloadSpeed = 2048,
                    Connections = 4,
                    State = DownloadState.Active,
                },
            ],
            WaitingItems = [],
            StoppedItems = [],
            PausedItems = [],
            CompleteItems = [],
            FailedItems =
            [
                new DownloadItem
                {
                    Gid = "g2",
                    Name = "missing.bin",
                    State = DownloadState.Failed,
                    ErrorCode = 3,
                    ErrorMessage = "Resource not found",
                },
            ],
            SkippedItems = [],
            InterruptedItems = [],
            AllItems = [],
        };
        var renderable = DashboardView.Build(state, TimeSpan.FromSeconds(30));
        var sw = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.Yes,
            ColorSystem = ColorSystemSupport.TrueColor,
            Out = new AnsiConsoleOutput(sw),
            Interactive = InteractionSupport.No,
        });
        console.Write(renderable);
        var text = sw.ToString();
        Assert.Contains("bigfile.zip", text, StringComparison.Ordinal);
        Assert.Contains("missing.bin", text, StringComparison.Ordinal);
    }
}
