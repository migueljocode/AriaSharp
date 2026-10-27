
namespace AriaSharp.Tests;

public class IntegrationTests
{
    [Fact]
    public void Mock_DownloadSession_EndToEnd_Flow()
    {
        var empty = Array.Empty<DownloadItem>();
        var state = new SessionState
        {
            Timestamp = 1L,
            NumActive = 1,
            NumWaiting = 0,
            TotalDownloadSpeed = 1024,
            CompletedLength = 500,
            TotalLength = 1000,
            OverallPercent = 50.0,
            ActiveItems = empty,
            WaitingItems = empty,
            StoppedItems = empty,
            PausedItems = empty,
            CompleteItems = empty,
            FailedItems = empty,
            SkippedItems = empty,
            InterruptedItems = empty,
            AllItems = empty,
        };

        Assert.Equal(1, state.NumActive);
        Assert.Equal(0, state.NumWaiting);
        Assert.Equal(1024L, state.TotalDownloadSpeed);
    }

    [Fact]
    public void FormatHelpers_Sanitize_RemovesControlChars()
    {
        var result = Sanitize("hello\x01world");
        Assert.Equal("hello?world", result);
    }

    [Fact]
    public void FormatHelpers_MaskCredentials_HidesPassword()
    {
        var masked = MaskCredentials("https://user:pass@example.com/file");
        Assert.Contains("***@", masked, StringComparison.Ordinal);
        Assert.DoesNotContain("pass", masked, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatHelpers_ProtocolBadge_Magnet()
    {
        Assert.Equal("MAGNET", ProtocolBadge(DownloadProtocol.Magnet));
    }

    [Fact]
    public void RunOutcome_Interrupted_HasMessage()
    {
        var outcome = RunOutcome.Interrupted("ctrl-c");
        Assert.Equal("ctrl-c", outcome.Message);
    }
}