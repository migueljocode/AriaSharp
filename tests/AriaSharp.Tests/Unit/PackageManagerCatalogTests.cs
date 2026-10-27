namespace AriaSharp.Tests;

public class PackageManagerCatalogParseTests
{
    [Fact]
    public void AptCandidate_Present()
    {
        const string sample = """
            aria2:
              Installed: (none)
              Candidate: 1.37.0-1
              Version table:
            """;
        Assert.True(PackageManagerCatalog.HasAptCandidate(sample));
        Assert.True(PackageManagerCatalog.IsPackageAvailable(
            PackageProbeKind.AptCandidate, 0, sample, ""));
    }

    [Fact]
    public void AptCandidate_None()
    {
        const string sample = """
            aria2:
              Installed: (none)
              Candidate: (none)
              Version table:
            """;
        Assert.False(PackageManagerCatalog.HasAptCandidate(sample));
        Assert.False(PackageManagerCatalog.IsPackageAvailable(
            PackageProbeKind.AptCandidate, 0, sample, ""));
    }

    [Fact]
    public void NonEmptyOutput_RequiresText()
    {
        Assert.False(PackageManagerCatalog.IsPackageAvailable(
            PackageProbeKind.NonEmptyOutput, 0, "  ", ""));
        Assert.True(PackageManagerCatalog.IsPackageAvailable(
            PackageProbeKind.NonEmptyOutput, 0, "aria2 1.37.0", ""));
        Assert.False(PackageManagerCatalog.IsPackageAvailable(
            PackageProbeKind.NonEmptyOutput, 1, "aria2", ""));
    }

    [Fact]
    public void ExitCodeZero_IgnoresBody()
    {
        Assert.True(PackageManagerCatalog.IsPackageAvailable(
            PackageProbeKind.ExitCodeZero, 0, "", ""));
        Assert.False(PackageManagerCatalog.IsPackageAvailable(
            PackageProbeKind.ExitCodeZero, 1, "ok", ""));
    }
}
