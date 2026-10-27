namespace AriaSharp.Tests;

public class PrerequisiteInstallerTests
{
    [Fact]
    public void ResolveInstallCommand_NoElevation_ReturnsDirect()
    {
        var mgr = new PackageManagerEntry(
            "brew", "brew", "brew", "info aria2", "brew", "install aria2",
            NeedsElevation: false, PackageProbeKind.ExitCodeZero, static () => true);
        var resolved = PrerequisiteInstaller.ResolveInstallCommand(mgr, nonInteractive: true);
        Assert.NotNull(resolved);
        Assert.Equal("brew", resolved.Value.File);
        Assert.Equal("install aria2", resolved.Value.Args);
    }

    [Fact]
    public void ResolveInstallCommand_NonInteractive_UsesSudoDashN()
    {
        if (OperatingSystem.IsWindows() || PrerequisiteInstaller.IsPrivileged())
            return;
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
    public void ResolveInstallCommand_Interactive_UsesPlainSudo()
    {
        if (OperatingSystem.IsWindows() || PrerequisiteInstaller.IsPrivileged())
            return;
        if (!PackageManagerCatalog.CommandExists("sudo"))
            return;

        var mgr = new PackageManagerEntry(
            "apt", "apt", "apt-cache", "policy aria2", "apt-get", "install -y aria2",
            NeedsElevation: true, PackageProbeKind.AptCandidate, static () => true);
        var resolved = PrerequisiteInstaller.ResolveInstallCommand(mgr, nonInteractive: false);
        Assert.NotNull(resolved);
        Assert.Equal("sudo", resolved.Value.File);
        Assert.False(resolved.Value.Args.StartsWith("-n ", StringComparison.Ordinal));
    }

    [Fact]
    public void ClassifyManagerFailure_LockPhrase_IsBusy()
    {
        Assert.Equal(
            InstallFailureKind.ManagerBusy,
            PrerequisiteInstaller.ClassifyManagerFailure(100, "Could not get lock /var/lib/dpkg/lock"));
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
    public void FormatInstallCommand_IncludesPackage()
    {
        var mgr = new PackageManagerEntry(
            "brew", "brew", "brew", "info aria2", "brew", "install aria2",
            NeedsElevation: false, PackageProbeKind.ExitCodeZero, static () => true);
        var text = PrerequisiteInstaller.FormatInstallCommand(mgr);
        Assert.Contains("install aria2", text, StringComparison.Ordinal);
    }
}

public class NetworkProbeTests
{
    [Fact]
    public async Task Override_Offline()
    {
        var prev = Environment.GetEnvironmentVariable("ARIASHARP_NETWORK");
        Environment.SetEnvironmentVariable("ARIASHARP_NETWORK", "offline");
        try
        {
            var (ok, detail) = await NetworkProbe.CheckAsync(CancellationToken.None);
            Assert.False(ok);
            Assert.Contains("offline", detail, StringComparison.OrdinalIgnoreCase);

            var (relOk, _) = await NetworkProbe.CanReachReleaseHostAsync(CancellationToken.None);
            Assert.False(relOk);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ARIASHARP_NETWORK", prev);
        }
    }

    [Fact]
    public async Task Override_Online()
    {
        var prev = Environment.GetEnvironmentVariable("ARIASHARP_NETWORK");
        Environment.SetEnvironmentVariable("ARIASHARP_NETWORK", "online");
        try
        {
            var (ok, _) = await NetworkProbe.CheckAsync(CancellationToken.None);
            Assert.True(ok);
            var (relOk, _) = await NetworkProbe.CanReachReleaseHostAsync(CancellationToken.None);
            Assert.True(relOk);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ARIASHARP_NETWORK", prev);
        }
    }

    [Fact]
    public void HasProxyEnv_DetectsHttpsProxy()
    {
        var prev = Environment.GetEnvironmentVariable("HTTPS_PROXY");
        Environment.SetEnvironmentVariable("HTTPS_PROXY", "http://127.0.0.1:8080");
        try
        {
            Assert.True(NetworkProbe.HasProxyEnv());
        }
        finally
        {
            Environment.SetEnvironmentVariable("HTTPS_PROXY", prev);
        }
    }
}
