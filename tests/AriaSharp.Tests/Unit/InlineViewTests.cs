using System.Text;

using AriaSharp.App;
using AriaSharp.Models;
using Xunit;

namespace AriaSharp.Tests;

public class InlineViewTests
{
    [Fact]
    public void WriteProgress_EmitsLineWithPercent()
    {
        var state = new SessionState
        {
            NumActive = 1, OverallPercent = 33.3, TotalDownloadSpeed = 4096,
            CompletedLength = 333, TotalLength = 1000,
            ActiveItems =
            [
                new DownloadItem { Gid = "g", Name = "payload.bin", State = DownloadState.Active, DownloadSpeed = 4096 },
            ],
            WaitingItems = [], StoppedItems = [], PausedItems = [],
            CompleteItems = [], FailedItems = [], SkippedItems = [], InterruptedItems = [], AllItems = [],
        };
        using var sw = new StringWriter();
        InlineView.WriteProgress(sw, state, TimeSpan.FromSeconds(9), color: false);
        var text = sw.ToString();
        Assert.Contains("33.3%", text, StringComparison.Ordinal);
        Assert.Contains("payload.bin", text, StringComparison.Ordinal);
        Assert.Contains("a=1", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_AtNarrowWidth_DoesNotThrow()
    {
        var state = new SessionState
        {
            ActiveItems =
            [
                new DownloadItem { Gid = "x", Name = "file.dat", TotalLength = 100, CompletedLength = 50 },
            ],
            WaitingItems = [], StoppedItems = [], PausedItems = [],
            CompleteItems = [], FailedItems = [], SkippedItems = [], InterruptedItems = [], AllItems = [],
        };
        var sb = new StringBuilder();
        InlineView.Render(sb, state, width: 40);
        Assert.Contains("file.dat", sb.ToString(), StringComparison.Ordinal);
        Assert.Contains("50.0%", sb.ToString(), StringComparison.Ordinal);
    }
}
