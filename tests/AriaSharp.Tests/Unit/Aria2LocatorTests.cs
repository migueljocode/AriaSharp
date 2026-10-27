
namespace AriaSharp.Tests;

public class Aria2LocatorTests
{
    [Fact]
    public async Task Locate_ExplicitMissing_Throws()
    {
        var locator = new Aria2Locator("/nonexistent/aria2c-does-not-exist");
        await Assert.ThrowsAsync<FileNotFoundException>(() => locator.LocateAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Locate_WhenInstalled_ReturnsPath()
    {
        // CI/sandbox usually has aria2c; if not, skip-style assert via catch
        var locator = new Aria2Locator();
        try
        {
            var path = await locator.LocateAsync(CancellationToken.None);
            Assert.False(string.IsNullOrWhiteSpace(path));
            Assert.True(File.Exists(path));
        }
        catch (FileNotFoundException)
        {
            // Acceptable on hosts without aria2
        }
    }
}
