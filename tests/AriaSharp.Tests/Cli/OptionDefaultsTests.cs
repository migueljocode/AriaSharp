
namespace AriaSharp.Tests;

public class OptionDefaultsTests
{
    [Theory]
    [InlineData("jobs", "2")]
    [InlineData("connections", "16")]
    [InlineData("split", "16")]
    [InlineData("per-server", "16")]
    [InlineData("min-split-size", "1M")]
    [InlineData("refresh", "500ms")]
    public void For_KnownOption_ReturnsItsDefault(string option, string expected) =>
        Assert.Equal(expected, OptionDefaults.For(option));

    [Fact]
    public void For_OptionWithoutDefault_ReturnsNull() =>
        Assert.Null(OptionDefaults.For("limit"));

    [Fact]
    public void PlainHelp_ShowsTheMinSplitSizeDefault()
    {
        var line = HelpDocument.Render(new AppSettings())
            .Split('\n')
            .First(l => l.Contains("--min-split-size", StringComparison.Ordinal));

        Assert.Contains("(default: 1M)", line, StringComparison.Ordinal);
    }
}
