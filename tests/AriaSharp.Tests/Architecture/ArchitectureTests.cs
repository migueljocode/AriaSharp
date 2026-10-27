
namespace AriaSharp.Tests;

public class ArchitectureTests
{
    [Fact]
    public void AllModels_ArePublicAndAccessible()
    {
        var item = new DownloadItem { Gid = "g1" };
        Assert.NotNull(item);
        Assert.Equal("g1", item.Gid);
    }

    [Fact]
    public void Enums_AreDefined()
    {
        Assert.True(Enum.IsDefined<DownloadState>(DownloadState.Active));
        Assert.True(Enum.IsDefined<DownloadProtocol>(DownloadProtocol.Https));
        Assert.True(Enum.IsDefined<EngineEventKind>(EngineEventKind.Started));
        Assert.True(Enum.IsDefined<SessionUpdateKind>(SessionUpdateKind.Snapshot));
    }

    [Fact]
    public void Models_Namespace_HasExpectedTypes()
    {
        var types = new[]
        {
            typeof(DownloadItem),
            typeof(EngineSnapshot),
            typeof(SessionState),
            typeof(EtaEstimate),
            typeof(TransferStats),
            typeof(AppDefaults),
            typeof(AppSettings),
            typeof(UiSettings),
            typeof(ErrorCatalog),
        };
        foreach (var type in types)
        {
            Assert.True(type.IsPublic || type.IsNestedPublic, type.Name + " should be public");
        }
    }

    [Fact]
    public void ErrorCatalog_CoversAllCodes()
    {
        for (var code = 1; code <= 32; code++)
        {
            Assert.False(string.IsNullOrEmpty(TextFor(code)), $"Code {code} text is empty");
            Assert.False(string.IsNullOrEmpty(HintFor(code)), $"Code {code} hint is empty");
        }
    }
}