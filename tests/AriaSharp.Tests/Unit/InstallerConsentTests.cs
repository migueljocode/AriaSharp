namespace AriaSharp.Tests;

public class InstallerConsentTests
{
    private static PackageManagerEntry AptMgr(bool available = true) =>
        new("apt", "apt", "apt-cache", "policy aria2", "apt-get", "install -y aria2",
            NeedsElevation: true, PackageProbeKind.AptCandidate, static () => true);

    private static PrerequisiteInstaller Create(
        FakeProcessRunner processes,
        FakeInstallEnvironment env,
        FakeNetworkProbe network,
        FakeArtifactInstaller artifacts,
        FakeLocator locator,
        IReadOnlyList<PackageManagerEntry>? managers = null) =>
        new(processes, env, network, artifacts, locator, managers);

    [Fact]
    public async Task ManagerAvailable_ConfirmDeclines_ReturnsAuth_NoInstall_NoFallback()
    {
        var processes = new FakeProcessRunner();
        processes.When("apt-cache", (_, _) => new ProcessRunResult(0, "Candidate: 1.37.0\n", ""));
        var artifacts = new FakeArtifactInstaller();
        var env = new FakeInstallEnvironment { IsPrivileged = true, Commands = ["apt-cache", "apt-get"] };
        var installer = Create(processes, env, new FakeNetworkProbe { ReleaseOk = true }, artifacts,
            new FakeLocator { Missing = true }, [AptMgr()]);

        var (ok, message, kind) = await installer.RunInstallAria2Async(
            autoYes: false, nonInteractive: true, CancellationToken.None, confirm: _ => false);

        Assert.False(ok);
        Assert.Equal(InstallFailureKind.Auth, kind);
        Assert.Contains("Refused", message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(processes.InstallRuns);
        Assert.Equal(0, artifacts.Calls);
    }

    [Fact]
    public async Task ManagerAvailable_ConfirmAccepts_RunsInstallOnce()
    {
        var processes = new FakeProcessRunner();
        processes.When("apt-cache", (_, _) => new ProcessRunResult(0, "Candidate: 1.37.0\n", ""));
        processes.When("apt-get", (_, _) => new ProcessRunResult(0, "ok", ""));
        var artifacts = new FakeArtifactInstaller();
        var locator = new FakeLocator { MissingUntil = 1 }; // missing for first locate, found after install
        var env = new FakeInstallEnvironment { IsPrivileged = true, Commands = ["apt-cache", "apt-get"] };
        var installer = Create(processes, env, new FakeNetworkProbe { ReleaseOk = true }, artifacts,
            locator, [AptMgr()]);

        var (ok, message, kind) = await installer.RunInstallAria2Async(
            autoYes: false, nonInteractive: true, CancellationToken.None, confirm: _ => true);

        Assert.True(ok);
        Assert.Equal(InstallFailureKind.None, kind);
        Assert.Contains("apt", message, StringComparison.OrdinalIgnoreCase);
        Assert.Single(processes.InstallRuns);
        Assert.Equal(0, artifacts.Calls);
    }

    [Fact]
    public async Task NoManager_FallbackDeclined_ReturnsAuth_NoArtifact()
    {
        var processes = new FakeProcessRunner();
        var artifacts = new FakeArtifactInstaller();
        var env = new FakeInstallEnvironment();
        var installer = Create(processes, env, new FakeNetworkProbe { ReleaseOk = true }, artifacts,
            new FakeLocator { Missing = true }, managers: []);

        var (ok, message, kind) = await installer.RunInstallAria2Async(
            autoYes: false, nonInteractive: true, CancellationToken.None, confirm: _ => false);

        Assert.False(ok);
        Assert.Equal(InstallFailureKind.Auth, kind);
        Assert.Contains("Refused", message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, artifacts.Calls);
    }

    [Fact]
    public async Task NoManager_AutoYes_CallsArtifactWithoutPrompt()
    {
        var processes = new FakeProcessRunner();
        var artifacts = new FakeArtifactInstaller { Ok = true };
        var locator = new FakeLocator { MissingUntil = 1 };
        var installer = Create(processes, new FakeInstallEnvironment(),
            new FakeNetworkProbe { ReleaseOk = true }, artifacts, locator, managers: []);

        var prompts = 0;
        var (ok, message, kind) = await installer.RunInstallAria2Async(
            autoYes: true, nonInteractive: true, CancellationToken.None,
            confirm: _ => { prompts++; return false; });

        Assert.True(ok);
        Assert.Equal(InstallFailureKind.None, kind);
        Assert.Equal(1, artifacts.Calls);
        Assert.Equal(0, prompts);
        Assert.Contains("GitHub", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReleaseHostUnreachable_ManagersTriedFirst_ThenNetwork()
    {
        var processes = new FakeProcessRunner();
        processes.When("apt-cache", (_, _) => new ProcessRunResult(0, "Candidate: (none)\n", ""));
        var artifacts = new FakeArtifactInstaller();
        var installer = Create(processes,
            new FakeInstallEnvironment { Commands = ["apt-cache", "apt-get"] },
            new FakeNetworkProbe { ReleaseOk = false, ReleaseDetail = "HEAD failed" },
            artifacts, new FakeLocator { Missing = true }, [AptMgr()]);

        var (ok, message, kind) = await installer.RunInstallAria2Async(
            autoYes: true, nonInteractive: true, CancellationToken.None);

        Assert.False(ok);
        Assert.Equal(InstallFailureKind.Network, kind);
        Assert.Contains("ARIASHARP_NETWORK=online", message, StringComparison.Ordinal);
        Assert.Equal(0, artifacts.Calls);
        Assert.Contains(processes.AllRuns, r => r.File == "apt-cache");
    }

    [Fact]
    public void ResolveInstallCommand_SudoVariants()
    {
        var mgr = AptMgr();
        var envPriv = new FakeInstallEnvironment { IsPrivileged = true, Commands = ["sudo", "apt-get"] };
        var direct = PrerequisiteInstaller.ResolveInstallCommand(mgr, nonInteractive: true, envPriv);
        Assert.NotNull(direct);
        Assert.Equal("apt-get", direct.Value.File);

        var envSudo = new FakeInstallEnvironment { IsPrivileged = false, Commands = ["sudo", "apt-get"] };
        var withN = PrerequisiteInstaller.ResolveInstallCommand(mgr, nonInteractive: true, envSudo);
        Assert.NotNull(withN);
        Assert.Equal("sudo", withN.Value.File);
        Assert.StartsWith("-n ", withN.Value.Args, StringComparison.Ordinal);

        var plain = PrerequisiteInstaller.ResolveInstallCommand(mgr, nonInteractive: false, envSudo);
        Assert.NotNull(plain);
        Assert.Equal("sudo", plain.Value.File);
        Assert.False(plain.Value.Args.StartsWith("-n ", StringComparison.Ordinal));

        var noSudo = new FakeInstallEnvironment { IsPrivileged = false, Commands = ["apt-get"] };
        Assert.Null(PrerequisiteInstaller.ResolveInstallCommand(mgr, nonInteractive: true, noSudo));
    }

    [Fact]
    public async Task ManagerFails_ThenFallback_Reached()
    {
        var processes = new FakeProcessRunner();
        processes.When("apt-cache", (_, _) => new ProcessRunResult(0, "Candidate: 1.37.0\n", ""));
        processes.When("apt-get", (_, _) => new ProcessRunResult(100, "", "Could not get lock"));
        var artifacts = new FakeArtifactInstaller { Ok = true };
        var locator = new FakeLocator { MissingUntil = 1 };
        using var log = new StringWriter();
        var installer = Create(processes,
            new FakeInstallEnvironment { IsPrivileged = true, Commands = ["apt-cache", "apt-get"] },
            new FakeNetworkProbe { ReleaseOk = true }, artifacts, locator, [AptMgr()]);

        var (ok, message, kind) = await installer.RunInstallAria2Async(
            autoYes: true, nonInteractive: true, CancellationToken.None, log: log);

        Assert.True(ok);
        Assert.Equal(1, artifacts.Calls);
        Assert.Contains("ManagerBusy", log.ToString(), StringComparison.Ordinal);
        Assert.Contains("falling back", log.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ManagerExit0_ButAria2StillMissing_FallsBack()
    {
        var processes = new FakeProcessRunner();
        processes.When("apt-cache", (_, _) => new ProcessRunResult(0, "Candidate: 1.37.0\n", ""));
        processes.When("apt-get", (_, _) => new ProcessRunResult(0, "ok", ""));
        var artifacts = new FakeArtifactInstaller { Ok = true };
        var locator = new FakeLocator { Missing = true }; // always missing
        using var log = new StringWriter();
        var installer = Create(processes,
            new FakeInstallEnvironment { IsPrivileged = true, Commands = ["apt-cache", "apt-get"] },
            new FakeNetworkProbe { ReleaseOk = true }, artifacts, locator, [AptMgr()]);

        var (ok, _, _) = await installer.RunInstallAria2Async(
            autoYes: true, nonInteractive: true, CancellationToken.None, log: log);

        Assert.True(ok);
        Assert.Equal(1, artifacts.Calls);
        Assert.Contains("exit 0 but aria2c not found", log.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed class FakeProcessRunner : IProcessRunner
{
    private readonly List<(string Prefix, Func<string, string, ProcessRunResult> Handler)> _handlers = [];
    public List<(string File, string Args)> AllRuns { get; } = [];
    public List<(string File, string Args)> InstallRuns { get; } = [];

    public void When(string filePrefix, Func<string, string, ProcessRunResult> handler) =>
        _handlers.Add((filePrefix, handler));

    public Task<ProcessRunResult> RunAsync(string fileName, string arguments, CancellationToken ct, string? workingDirectory = null)
    {
        AllRuns.Add((fileName, arguments));
        if (fileName is "apt-get" or "sudo" || arguments.Contains("install", StringComparison.Ordinal))
            InstallRuns.Add((fileName, arguments));

        foreach (var (prefix, handler) in _handlers)
        {
            if (fileName.Equals(prefix, StringComparison.OrdinalIgnoreCase)
                || (fileName == "sudo" && arguments.Contains(prefix, StringComparison.Ordinal)))
                return Task.FromResult(handler(fileName, arguments));
        }

        return Task.FromResult(new ProcessRunResult(1, "", "no handler"));
    }
}

internal sealed class FakeInstallEnvironment : IInstallEnvironment
{
    public bool IsWindows { get; set; }
    public bool IsPrivileged { get; set; }
    public List<string> Commands { get; set; } = [];
    public bool CommandExists(string name) =>
        Commands.Contains(name, StringComparer.OrdinalIgnoreCase);
}

internal sealed class FakeNetworkProbe : INetworkProbe
{
    public bool CheckOk { get; set; } = true;
    public bool ReleaseOk { get; set; } = true;
    public string ReleaseDetail { get; set; } = "ok";

    public Task<(bool Ok, string Detail)> CheckAsync(CancellationToken ct) =>
        Task.FromResult((CheckOk, CheckOk ? "ok" : "fail"));

    public Task<(bool Ok, string Detail)> CanReachReleaseHostAsync(CancellationToken ct) =>
        Task.FromResult((ReleaseOk, ReleaseDetail));
}

internal sealed class FakeArtifactInstaller : IArtifactInstaller
{
    public bool Ok { get; set; } = true;
    public int Calls { get; private set; }

    public Task<(bool Ok, string Message, string? LogPath)> InstallAsync(string cacheDirectory, CancellationToken ct)
    {
        Calls++;
        return Task.FromResult(Ok
            ? (true, "fake artifact ok", (string?)null)
            : (false, "fake artifact fail", (string?)null));
    }
}

internal sealed class FakeLocator : IAria2Locator
{
    public bool Missing { get; set; }
    public int MissingUntil { get; set; }
    private int _calls;

    public Task<string> LocateAsync(CancellationToken ct)
    {
        var n = Interlocked.Increment(ref _calls);
        if (Missing || n <= MissingUntil)
            throw new FileNotFoundException("aria2c not found");
        return Task.FromResult("/fake/aria2c");
    }
}
