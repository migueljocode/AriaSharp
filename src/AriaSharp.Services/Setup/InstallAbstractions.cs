namespace AriaSharp.Services;

public readonly record struct ProcessRunResult(int ExitCode, string StdOut, string StdErr);

internal interface IProcessRunner
{
    Task<ProcessRunResult> RunAsync(
        string fileName,
        string arguments,
        CancellationToken ct,
        string? workingDirectory = null);
}

internal interface IInstallEnvironment
{
    bool IsWindows { get; }
    bool IsPrivileged { get; }
    bool CommandExists(string name);
}

internal interface INetworkProbe
{
    Task<(bool Ok, string Detail)> CheckAsync(CancellationToken ct);
    Task<(bool Ok, string Detail)> CanReachReleaseHostAsync(CancellationToken ct);
}

internal interface IArtifactInstaller
{
    Task<(bool Ok, string Message, string? LogPath)> InstallAsync(string cacheDirectory, CancellationToken ct);
}

/// <summary>Default OS / privilege / PATH probes for production install.</summary>
internal sealed class DefaultInstallEnvironment : IInstallEnvironment
{
    public bool IsWindows => OperatingSystem.IsWindows();

    public bool IsPrivileged
    {
        get
        {
            try { return Environment.IsPrivilegedProcess; }
            catch (Exception ex) when (ex is PlatformNotSupportedException or InvalidOperationException)
            {
                return false;
            }
        }
    }

    public bool CommandExists(string name) => PackageManagerCatalog.CommandExists(name);
}

/// <summary>Adapts static NetworkProbe helpers to <see cref="INetworkProbe"/>.</summary>
internal sealed class DefaultNetworkProbe : INetworkProbe
{
    public Task<(bool Ok, string Detail)> CheckAsync(CancellationToken ct) =>
        NetworkProbe.CheckAsync(ct);

    public Task<(bool Ok, string Detail)> CanReachReleaseHostAsync(CancellationToken ct) =>
        NetworkProbe.CanReachReleaseHostAsync(ct);
}

/// <summary>Adapts Aria2GitHubInstaller to <see cref="IArtifactInstaller"/>.</summary>
internal sealed class DefaultArtifactInstaller : IArtifactInstaller
{
    public Task<(bool Ok, string Message, string? LogPath)> InstallAsync(string cacheDirectory, CancellationToken ct) =>
        Aria2GitHubInstaller.InstallForCurrentOsAsync(cacheDirectory, ct);
}

/// <summary>Adapts static ProcessRunner to <see cref="IProcessRunner"/>.</summary>
internal sealed class DefaultProcessRunner : IProcessRunner
{
    public async Task<ProcessRunResult> RunAsync(
        string fileName,
        string arguments,
        CancellationToken ct,
        string? workingDirectory = null)
    {
        return await ProcessRunner.RunAsync(fileName, arguments, ct, workingDirectory);
    }
}
