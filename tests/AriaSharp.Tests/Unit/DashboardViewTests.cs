using Spectre.Console;
using Spectre.Console.Rendering;

namespace AriaSharp.Tests;

public class DashboardViewTests
{
    [Fact]
    public void Build_EmptyState_ReturnsRenderable()
    {
        var renderable = DashboardView.Build(EmptyState(), TimeSpan.FromSeconds(1));
        Assert.NotNull(renderable);
        _ = RenderToString(renderable, width: 80);
    }

    [Fact]
    public void Build_WithActiveAndFailed_RendersNames()
    {
        var text = RenderToString(DashboardView.Build(SampleState(), TimeSpan.FromSeconds(30)), width: 100);
        Assert.Contains("bigfile.zip", text, StringComparison.Ordinal);
        Assert.Contains("missing.bin", text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(40)]
    [InlineData(80)]
    [InlineData(120)]
    public void Build_AtWidth_DoesNotThrow(int width)
    {
        var text = RenderToString(DashboardView.Build(SampleState(), TimeSpan.FromSeconds(5)), width);
        Assert.False(string.IsNullOrWhiteSpace(text));
        Assert.Contains("ACTIVE", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_NarrowWidth_StillContainsStatus()
    {
        var text = RenderToString(DashboardView.Build(SampleState(), TimeSpan.Zero), width: 40);
        Assert.Contains("complete", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_HeadersAreSplitAndConnection()
    {
        var text = RenderPlain(DashboardView.Build(SampleState(), TimeSpan.FromSeconds(5), 16, splitPerServer: 16), width: 120);
        Assert.Contains("Split", text, StringComparison.Ordinal);
        Assert.Contains("Connection", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_ConnectionCell_ShowsLiveOverMax()
    {
        var text = RenderPlain(DashboardView.Build(SampleState(), TimeSpan.FromSeconds(5), 16, splitPerServer: 16), width: 120);
        Assert.Contains("4/16", text, StringComparison.Ordinal);
    }

    private static string RenderPlain(IRenderable renderable, int width)
    {
        var sw = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(sw),
            Interactive = InteractionSupport.No,
        });
        console.Profile.Width = width;
        console.Write(renderable);
        return sw.ToString();
    }

    private static string RenderToString(IRenderable renderable, int width)
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
        console.Write(renderable);
        return sw.ToString();
    }

    private static SessionState EmptyState() => new()
    {
        ActiveItems = [], WaitingItems = [], StoppedItems = [], PausedItems = [],
        CompleteItems = [], FailedItems = [], SkippedItems = [], InterruptedItems = [], AllItems = [],
    };

    private static SessionState SampleState() => new()
    {
        NumActive = 1, OverallPercent = 50, TotalDownloadSpeed = 2048,
        CompletedLength = 500, TotalLength = 1000,
        ActiveItems =
        [
            new DownloadItem
            {
                Gid = "g1", Name = "bigfile.zip", CompletedLength = 500, TotalLength = 1000,
                DownloadSpeed = 2048, Connections = 4, State = DownloadState.Active,
            },
        ],
        WaitingItems = [], StoppedItems = [], PausedItems = [], CompleteItems = [],
        FailedItems =
        [
            new DownloadItem
            {
                Gid = "g2", Name = "missing.bin", State = DownloadState.Failed,
                ErrorCode = 3, ErrorMessage = "Resource not found",
            },
        ],
        SkippedItems = [], InterruptedItems = [], AllItems = [],
    };
}
