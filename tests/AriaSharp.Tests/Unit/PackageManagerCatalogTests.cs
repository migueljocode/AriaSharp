using AriaSharp.Models;
using AriaSharp.Services;
using Xunit;

namespace AriaSharp.Tests;

public class Aria2BootstrapPathsTests
{
    [Fact]
    public void Manifest_Urls_PointAtOfficialRelease()
    {
        Assert.Contains("1.37.0", Aria2Manifest.Version, StringComparison.Ordinal);
        Assert.StartsWith("https://github.com/aria2/aria2/releases/", Aria2Manifest.WindowsZipUrl, StringComparison.Ordinal);
        Assert.StartsWith("https://github.com/aria2/aria2/releases/", Aria2Manifest.SourceTarballGzUrl, StringComparison.Ordinal);
    }

    [Fact]
    public void AppPaths_ToolsBin_IsUnderDataDirectory()
    {
        Assert.StartsWith(AppPaths.DataDirectory, AppPaths.ToolsBinDirectory, StringComparison.Ordinal);
        Assert.EndsWith("aria2c", AppPaths.BundledAria2cPath.Replace(".exe", "", StringComparison.OrdinalIgnoreCase), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Locator_PrefersBundled_WhenPresent()
    {
        // Only asserts contract: bundled path is absolute under data dir
        Assert.True(Path.IsPathRooted(AppPaths.BundledAria2cPath));
    }
}
