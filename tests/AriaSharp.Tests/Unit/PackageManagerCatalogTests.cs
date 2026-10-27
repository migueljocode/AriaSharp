using AriaSharp.Services;
using Xunit;

namespace AriaSharp.Tests;

public class PackageManagerCatalogTests
{
    [Theory]
    [InlineData("aria2", "winget", "aria2.aria2")]
    [InlineData("aria2c", "brew", "aria2")]
    [InlineData("aria2", "apt", "aria2")]
    [InlineData("xclip", "apt", "xclip")]
    public void MapPackage_LogicalNames(string logical, string manager, string expected)
    {
        Assert.Equal(expected, PackageManagerCatalog.MapPackage(logical, manager));
    }

    [Fact]
    public void Managers_CoverMajorPlatforms()
    {
        Assert.Contains(PackageManagerCatalog.LinuxManagers, m => m.Id == "apt");
        Assert.Contains(PackageManagerCatalog.LinuxManagers, m => m.Id == "pacman");
        Assert.Contains(PackageManagerCatalog.MacManagers, m => m.Id == "brew");
        Assert.Contains(PackageManagerCatalog.WindowsManagers, m => m.Id == "winget");
        Assert.Contains(PackageManagerCatalog.WindowsManagers, m => m.Id == "choco");
    }
}
