namespace AriaSharp.Tests;

public class InputCommandTests
{
    [Fact]
    public async Task InvalidArgs_ReturnsUsageError_RejectedMessage()
    {
        var settings = new AppSettings { Inputs = ["not-a-url", "also-not-a-file"] };
        var (exit, resolved) = await InputCommand.ResolveAsync(settings);
        Assert.Equal((int)ExitCode.UsageError, exit);
        Assert.Empty(resolved.Inputs);
    }

    [Fact]
    public async Task ValidUrl_ReturnsNoExit()
    {
        var settings = new AppSettings { Inputs = ["https://example.com/file.zip"] };
        var (exit, resolved) = await InputCommand.ResolveAsync(settings);
        Assert.Null(exit);
        Assert.Single(resolved.Inputs);
        Assert.Equal("https://example.com/file.zip", resolved.Inputs[0]);
    }
}
