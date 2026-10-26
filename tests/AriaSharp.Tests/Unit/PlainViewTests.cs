using System.Text;

using AriaSharp.App;
using AriaSharp.Models;
using Xunit;

namespace AriaSharp.Tests;

public class PlainViewTests
{
    [Fact]
    public void WriteProgress_Interactive_UsesCarriageReturn()
    {
        var state = new SessionState
        {
            NumActive = 1,
            OverallPercent = 42.5,
            TotalDownloadSpeed = 1024,
            CompletedLength = 512,
            TotalLength = 1024,
            ActiveItems = [new DownloadItem { Gid = "g1", Name = "file.bin", State = DownloadState.Active }],
            WaitingItems = [],
            StoppedItems = [],
            PausedItems = [],
            CompleteItems = [],
            FailedItems = [],
            SkippedItems = [],
            InterruptedItems = [],
            AllItems = [],
        };
        using var sw = new StringWriter();
        PlainView.WriteProgress(sw, state, TimeSpan.FromSeconds(12), interactive: true);
        var text = sw.ToString();
        Assert.StartsWith("\r", text, StringComparison.Ordinal);
        Assert.Contains("42.5%", text, StringComparison.Ordinal);
        Assert.Contains("file.bin", text, StringComparison.Ordinal);
        Assert.Contains("a=1", text, StringComparison.Ordinal);
    }

    [Fact]
    public void WriteProgress_Redirected_WritesNewline()
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
        using var sw = new StringWriter();
        PlainView.WriteProgress(sw, state, TimeSpan.FromSeconds(1), interactive: false);
        var text = sw.ToString();
        Assert.EndsWith(Environment.NewLine, text, StringComparison.Ordinal);
        // Interactive path uses lone CR overwrite; redirected must end with a full newline
        // (Windows NewLine is \r\n, so we only forbid a trailing lone CR without LF).
        Assert.False(text.EndsWith('\r'));
    }
}
