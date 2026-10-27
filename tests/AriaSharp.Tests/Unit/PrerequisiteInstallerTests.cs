namespace AriaSharp.Tests;

public class PrerequisiteInstallerTests
{
    [Fact]
    public void ResolveInstallCommand_NoElevation_ReturnsDirect()
    {
        var mgr = new PackageManagerEntry(
            "brew", "brew", "brew", "info aria2", "brew", "install aria2",
            NeedsElevation: false, PackageProbeKind.ExitCodeZero, static () => true);
        var resolved = PrerequisiteInstaller.ResolveInstallCommand(mgr);
        Assert.NotNull(resolved);
        Assert.Equal("brew", resolved.Value.File);
        Assert.Equal("install aria2", resolved.Value.Args);
    }

    [Fact]
    public void ResolveInstallCommand_NonInteractive_UsesSudoDashN()
    {
        if (OperatingSystem.IsWindows() || PrerequisiteInstaller.IsPrivileged())
            return; // path not applicable
        if (!PackageManagerCatalog.CommandExists("sudo"))
            return;

        var mgr = new PackageManagerEntry(
            "apt", "apt", "apt-cache", "policy aria2", "apt-get", "install -y aria2",
            NeedsElevation: true, PackageProbeKind.AptCandidate, static () => true);
        var resolved = PrerequisiteInstaller.ResolveInstallCommand(mgr, nonInteractive: true);
        Assert.NotNull(resolved);
        Assert.Equal("sudo", resolved.Value.File);
        Assert.StartsWith("-n ", resolved.Value.Args, StringComparison.Ordinal);
    }

    [Fact]
    public void ClassifyManagerFailure_LockPhrase_IsBusy()
    {
        Assert.Equal(
            InstallFailureKind.ManagerBusy,
            PrerequisiteInstaller.ClassifyManagerFailure(100, "Could not get lock /var/lib/dpkg/lock"));
        // substring "lock" alone in unrelated words must not match
        Assert.True(
            PrerequisiteInstaller.ClassifyManagerFailure(1, "blocked by policy") != InstallFailureKind.ManagerBusy);

    }

    [Fact]
    public void ClassifyManagerFailure_SudoNoTty_IsAuth()
    {
        Assert.Equal(
            InstallFailureKind.Auth,
            PrerequisiteInstaller.ClassifyManagerFailure(1, "sudo: a terminal is required to read the password"));
    }

    [Fact]
    public void ClassifyManagerFailure_Permission_IsAuth()
    {
        Assert.Equal(
            InstallFailureKind.Auth,
            PrerequisiteInstaller.ClassifyManagerFailure(1, "Permission denied"));
    }

    [Fact]
    public void ClassifyManagerFailure_Exit100_IsPackageMissing()
    {
        Assert.Equal(
            InstallFailureKind.PackageMissing,
            PrerequisiteInstaller.ClassifyManagerFailure(100, ""));
    }

    [Fact]
    public async Task Consent_Refused_OnFallback_ReturnsAuth()
    {
        // Force offline so we don't touch the network if already missing.
        // If aria2 is already installed, the method returns success early — skip.
        try
        {
            _ = await new Aria2Locator().LocateAsync(CancellationToken.None);
            return; // cannot test refuse path when installed
        }
        catch (FileNotFoundException) { }

        Environment.SetEnvironmentVariable("ARIASHARP_NETWORK", "online");
        try
        {
            var (ok, message, kind) = await PrerequisiteInstaller.InstallAria2Async(
                CancellationToken.None,
                autoYes: false,
                confirm: _ => false);
            Assert.False(ok);
            Assert.Equal(InstallFailureKind.Auth, kind);
            Assert.Contains("Refused", message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ARIASHARP_NETWORK", null);
        }
    }
}
