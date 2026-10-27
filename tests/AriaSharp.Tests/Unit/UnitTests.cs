
namespace AriaSharp.Tests;

public class UnitTests
{
    [Fact]
    public void FormatBytes_ReturnsBytesForSmallValues()
    {
        Assert.Equal("512 B", FormatHelpers.FormatBytes(512));
    }

    [Fact]
    public void FormatBytes_ReturnsKbForLargeValues()
    {
        var result = FormatHelpers.FormatBytes(2048);
        Assert.Contains("KB", result, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatSpeed_FormatsSpeedCorrectly()
    {
        var result = FormatHelpers.FormatSpeed(1024);
        Assert.Contains("/s", result, StringComparison.Ordinal);
    }

    [Fact]
    public void Truncate_CutsLongStrings()
    {
        var result = FormatHelpers.Truncate("abcdefghijklmnopqrstuvwxyz", 10);
        Assert.Equal("abcdefg...", result);
    }

    [Fact]
    public void Truncate_ReturnsShortStringsUnchanged()
    {
        var result = FormatHelpers.Truncate("short", 20);
        Assert.Equal("short", result);
    }

    [Fact]
    public void Truncate_SmallMaxDoesNotThrow()
    {
        Assert.Equal(string.Empty, FormatHelpers.Truncate("abc", 0));
        Assert.Equal("a", FormatHelpers.Truncate("abc", 1));
        Assert.Equal("ab", FormatHelpers.Truncate("abc", 2));
        Assert.Equal("abc", FormatHelpers.Truncate("abc", 3));
    }

    [Fact]
    public void ProtocolBadge_ReturnsCorrectLabels()
    {
        Assert.Equal("HTTPS", FormatHelpers.ProtocolBadge(DownloadProtocol.Https));
        Assert.Equal("HTTP", FormatHelpers.ProtocolBadge(DownloadProtocol.Http));
    }

    [Fact]
    public void EtaEstimate_Format_ExactShowsDuration()
    {
        var eta = EtaEstimate.Exact(3661);
        Assert.Equal("1:01:01", eta.Format());
    }

    [Fact]
    public void EtaEstimate_Format_UnknownReturnsDash()
    {
        Assert.Equal("--", EtaEstimate.Unknown.Format());
    }

    [Fact]
    public void DownloadState_Enum_HasExpectedMembers()
    {
        Assert.Equal(DownloadState.Waiting, DownloadState.Waiting);
        Assert.Equal(DownloadState.Active, DownloadState.Active);
    }

    [Fact]
    public void ExitCode_Enum_HasExpectedValues()
    {
        Assert.Equal(0, (int)ExitCode.Ok);
        Assert.Equal(1, (int)ExitCode.Failure);
        Assert.Equal(2, (int)ExitCode.UsageError);
        Assert.Equal(130, (int)ExitCode.Interrupted);
    }

    [Fact]
    public void RunOutcome_CreatedStates_AreDistinct()
    {
        Assert.Equal(RunOutcomeKind.Completed, RunOutcome.Completed().Kind);
        Assert.Equal(RunOutcomeKind.Interrupted, RunOutcome.Interrupted().Kind);
        Assert.Equal(RunOutcomeKind.Failed, RunOutcome.Failed().Kind);
        Assert.Equal(RunOutcomeKind.Empty, RunOutcome.Empty().Kind);
    }

    [Fact]
    public void Aria2ErrorInfo_None_HasDefaultValues()
    {
        var info = Aria2ErrorInfo.None;
        Assert.Equal(Aria2ErrorKind.None, info.Kind);
    }

    [Fact]
    public void ErrorCatalog_TextFor_ReturnsKnownText()
    {
        Assert.Equal("Unknown error", ErrorCatalog.TextFor(1));
        Assert.Contains("Unknown error", ErrorCatalog.TextFor(999), StringComparison.Ordinal);
    }

    [Fact]
    public void ErrorCatalog_HintFor_ReturnsHint()
    {
        Assert.NotEmpty(ErrorCatalog.HintFor(7));
    }

    [Fact]
    public void ErrorCatalog_Classify_Timeout()
    {
        Assert.Equal(Aria2ErrorKind.Timeout, ErrorCatalog.Classify(2));
    }

    [Fact]
    public void ErrorCatalog_Classify_Network()
    {
        Assert.Equal(Aria2ErrorKind.Network, ErrorCatalog.Classify(6));
    }

    [Fact]
    public void ErrorCatalog_Classify_Protocol()
    {
        Assert.Equal(Aria2ErrorKind.Protocol, ErrorCatalog.Classify(22));
    }

    [Fact]
    public void AppDefaults_Constants_HaveExpectedValues()
    {
        Assert.Equal(1, AppDefaults.MaxConcurrentDownloads);
        Assert.Equal(16, AppDefaults.Split);
        Assert.Equal(500, AppDefaults.RefreshIntervalMs);
    }

    [Fact]
    public void DownloadItem_CanBeCreatedWithDefaults()
    {
        var item = new DownloadItem { Gid = "abc" };
        Assert.Equal("abc", item.Gid);
        Assert.Equal(string.Empty, item.Name);
    }

    [Fact]
    public void EngineSnapshot_RequiresAllArrays()
    {
        var empty = Array.Empty<DownloadItem>();
        var snap = new EngineSnapshot
        {
            Timestamp = 1L,
            Active = empty, Waiting = empty, Stopped = empty, Paused = empty,
            Complete = empty, Failed = empty, Skipped = empty, Interrupted = empty,
        };
        Assert.Equal(1L, snap.Timestamp);
    }

    [Fact]
    public void EngineSnapshot_HasSpeedFields()
    {
        var empty = Array.Empty<DownloadItem>();
        var snap = new EngineSnapshot
        {
            Timestamp = 1L,
            Active = empty, Waiting = empty, Stopped = empty, Paused = empty,
            Complete = empty, Failed = empty, Skipped = empty, Interrupted = empty,
            NumActive = 1, TotalDownloadSpeed = 1024,
        };
        Assert.Equal(1024L, snap.TotalDownloadSpeed);
    }

    [Fact]
    public void SessionState_CalculatedProperties_AreAccessible()
    {
        var empty = Array.Empty<DownloadItem>();
        var state = new SessionState
        {
            Timestamp = 1L, NumActive = 2, NumWaiting = 3,
            TotalDownloadSpeed = 512, OverallPercent = 42.5,
            ActiveItems = empty, WaitingItems = empty, StoppedItems = empty, PausedItems = empty,
            CompleteItems = empty, FailedItems = empty, SkippedItems = empty,
            InterruptedItems = empty, AllItems = empty,
        };
        Assert.Equal(2, state.NumActive);
        Assert.Equal(42.5, state.OverallPercent);
    }

    [Fact]
    public void TransferStats_HasExpectedDefaults()
    {
        var stats = new TransferStats();
        Assert.Equal(0L, stats.Completed);
        Assert.Equal(EtaEstimate.Unknown.Format(), stats.Eta.Format());
    }
}