namespace AriaSharp.Services;

public enum InstallFailureKind
{
    None,
    Network,
    PackageMissing,
    ManagerBusy,
    Auth,
    BuildTools,
    HashMismatch,
    Unknown,
}

/// <summary>
/// Install aria2: package manager when available, else pinned GitHub artifact / source build.
/// Production code uses <see cref="CreateDefault"/>; tests inject fakes.
/// </summary>
public sealed class PrerequisiteInstaller : IPrerequisiteInstaller
{
    private readonly IProcessRunner _processes;
    private readonly IInstallEnvironment _env;
    private readonly INetworkProbe _network;
    private readonly IArtifactInstaller _artifacts;
    private readonly IAria2Locator _locator;
    private readonly IReadOnlyList<PackageManagerEntry> _managers;

    public PrerequisiteInstaller(
        IProcessRunner processes,
        IInstallEnvironment env,
        INetworkProbe network,
        IArtifactInstaller artifacts,
        IAria2Locator locator,
        IReadOnlyList<PackageManagerEntry>? managers = null)
    {
        _processes = processes ?? throw new ArgumentNullException(nameof(processes));
        _env = env ?? throw new ArgumentNullException(nameof(env));
        _network = network ?? throw new ArgumentNullException(nameof(network));
        _artifacts = artifacts ?? throw new ArgumentNullException(nameof(artifacts));
        _locator = locator ?? throw new ArgumentNullException(nameof(locator));
        _managers = managers ?? PackageManagerCatalog.Aria2Managers;
    }

    public static PrerequisiteInstaller CreateDefault(IAria2Locator? locator = null) =>
        new(
            new DefaultProcessRunner(),
            new DefaultInstallEnvironment(),
            new DefaultNetworkProbe(),
            new DefaultArtifactInstaller(),
            locator ?? new Aria2Locator());

    public async Task<bool> IsInstalledAsync(string package, CancellationToken ct)
    {
        var command = package switch
        {
            "wl-clipboard" => "wl-paste",
            "xclip" => "xclip",
            "xsel" => "xsel",
            "aria2" or "aria2c" => "aria2c",
            _ => package,
        };

        if (command is "aria2c" or "aria2")
        {
            try
            {
                _ = await _locator.LocateAsync(ct);
                return true;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
        }

        if (_env.IsWindows)
            return false;

        var result = await _processes.RunAsync("which", command, ct);
        return result.ExitCode == 0;
    }

    /// <summary>Production entry: default-wired installer.</summary>
    public static Task<(bool Ok, string Message, InstallFailureKind Kind)> InstallAria2Async(
        bool autoYes,
        bool nonInteractive,
        CancellationToken ct,
        Func<string, bool>? confirm = null,
        TextWriter? log = null) =>
        CreateDefault().RunInstallAria2Async(autoYes, nonInteractive, ct, confirm, log);

    public async Task<(bool Ok, string Message, InstallFailureKind Kind)> RunInstallAria2Async(
        bool autoYes,
        bool nonInteractive,
        CancellationToken ct,
        Func<string, bool>? confirm = null,
        TextWriter? log = null)
    {
        try
        {
            _ = await _locator.LocateAsync(ct);
            return (true, "already installed", InstallFailureKind.None);
        }
        catch (FileNotFoundException) { }

        var notes = new List<string>();

        foreach (var mgr in _managers.Where(m => m.IsOsMatch()))
        {
            ProcessRunResult probe;
            try
            {
                probe = await _processes.RunAsync(mgr.ProbeFileName, mgr.ProbeArgs, ct);
            }
            catch (Exception ex) when (ex is InvalidOperationException or IOException or System.ComponentModel.Win32Exception)
            {
                continue;
            }

            if (!PackageManagerCatalog.IsPackageAvailable(mgr.ProbeKind, probe.ExitCode, probe.StdOut, probe.StdErr))
                continue;

            var cmdDisplay = FormatInstallCommand(mgr, nonInteractive: false);
            if (!autoYes)
            {
                if (confirm is null || !confirm(cmdDisplay))
                {
                    notes.Add($"refused {mgr.DisplayName}; not falling back after explicit decline");
                    if (log is not null) await log.WriteLineAsync(notes[^1]);
                    return (false,
                        $"Refused install via {mgr.DisplayName}. Re-run with -y or run: {cmdDisplay} (fallback not used after decline)",
                        InstallFailureKind.Auth);
                }
            }

            var resolved = ResolveInstallCommand(mgr, nonInteractive, _env);
            if (resolved is null)
            {
                notes.Add($"{mgr.DisplayName}: needs root (no sudo); skipping");
                if (log is not null) await log.WriteLineAsync(notes[^1]);
                continue;
            }

            var (file, args) = resolved.Value;
            if (log is not null) await log.WriteLineAsync($"running: {file} {args}");
            var install = await _processes.RunAsync(file, args, ct);
            await WriteNonEmptyAsync(log, install.StdOut);
            await WriteNonEmptyAsync(log, install.StdErr);

            if (install.ExitCode == 0)
            {
                try
                {
                    _ = await _locator.LocateAsync(ct);
                    return (true, $"installed via {mgr.DisplayName}", InstallFailureKind.None);
                }
                catch (FileNotFoundException)
                {
                    notes.Add($"{mgr.DisplayName}: exit 0 but aria2c not found on PATH");
                    if (log is not null) await log.WriteLineAsync(notes[^1]);
                }
            }
            else
            {
                var kind = ClassifyManagerFailure(install.ExitCode, install.StdErr);
                notes.Add($"{mgr.DisplayName} failed ({kind}): {file} {args}");
                if (log is not null) await log.WriteLineAsync(notes[^1]);
            }
        }

        var (releaseOk, releaseDetail) = await _network.CanReachReleaseHostAsync(ct);
        if (!releaseOk)
        {
            var prefix = notes.Count > 0 ? string.Join("; ", notes) + "; " : "";
            return (false,
                prefix + $"cannot reach aria2 release host for fallback: {releaseDetail} — install via package manager, set --aria2-path, or ARIASHARP_NETWORK=online",
                InstallFailureKind.Network);
        }

        const string fallbackPlan = "download pinned official aria2 release (zip or source build)";
        if (!autoYes)
        {
            if (confirm is null || !confirm(fallbackPlan))
            {
                return (false,
                    $"Refused {fallbackPlan}. Re-run with -y/--install-deps or install aria2 manually.",
                    InstallFailureKind.Auth);
            }
        }

        if (log is not null) await log.WriteLineAsync("falling back to pinned official artifact / source build…");
        notes.Add("falling back to pinned official artifact / source build");

        AppPaths.EnsureDataDirectories();
        var cache = Path.Join(AppPaths.ToolsCacheDirectory, $"aria2-{Guid.NewGuid():N}");
        Directory.CreateDirectory(cache);
        try
        {
            var (ok, message, logPath) = await _artifacts.InstallAsync(cache, ct);
            if (!ok)
            {
                var kind = message.Contains("SHA-256", StringComparison.Ordinal) ? InstallFailureKind.HashMismatch
                    : message.Contains("build tools", StringComparison.OrdinalIgnoreCase)
                      || message.Contains("headers", StringComparison.OrdinalIgnoreCase)
                        ? InstallFailureKind.BuildTools
                        : InstallFailureKind.Unknown;
                var prefix = notes.Count > 0 ? string.Join("; ", notes) + "; " : "";
                return (false,
                    prefix + $"source/zip fallback failed: {message}"
                    + (logPath is not null ? $" (log: {logPath})" : "")
                    + $" — manual: install aria2 {Aria2Manifest.Version} or set --aria2-path",
                    kind);
            }
            return (true, "installed from official GitHub artifact", InstallFailureKind.None);
        }
        finally
        {
            TryDeleteTree(cache);
        }
    }

    public string FormatInstallCommand(PackageManagerEntry mgr, bool nonInteractive = false)
    {
        ArgumentNullException.ThrowIfNull(mgr);
        var resolved = ResolveInstallCommand(mgr, nonInteractive, _env);
        if (resolved is null)
            return $"{mgr.InstallFileName} {mgr.InstallArgs} (needs root)";
        return $"{resolved.Value.File} {resolved.Value.Args}";
    }

    public static string FormatInstallCommand(PackageManagerEntry mgr) =>
        CreateDefault().FormatInstallCommand(mgr, nonInteractive: false);

    public static InstallFailureKind ClassifyManagerFailure(int exitCode, string stdErr)
    {
        ArgumentNullException.ThrowIfNull(stdErr);
        if (stdErr.Contains("could not get lock", StringComparison.OrdinalIgnoreCase)
            || stdErr.Contains("Unable to acquire the dpkg frontend lock", StringComparison.OrdinalIgnoreCase))
            return InstallFailureKind.ManagerBusy;
        if (stdErr.Contains("a terminal is required", StringComparison.OrdinalIgnoreCase)
            || stdErr.Contains("no tty", StringComparison.OrdinalIgnoreCase)
            || stdErr.Contains("password is required", StringComparison.OrdinalIgnoreCase)
            || stdErr.Contains("permission denied", StringComparison.OrdinalIgnoreCase)
            || exitCode is 126 or 127)
            return InstallFailureKind.Auth;
        if (exitCode is 1 or 100)
            return InstallFailureKind.PackageMissing;
        return InstallFailureKind.Unknown;
    }

    public static (string File, string Args)? ResolveInstallCommand(
        PackageManagerEntry mgr,
        bool nonInteractive = true) =>
        ResolveInstallCommand(mgr, nonInteractive, new DefaultInstallEnvironment());

    public static (string File, string Args)? ResolveInstallCommand(
        PackageManagerEntry mgr,
        bool nonInteractive,
        IInstallEnvironment env)
    {
        ArgumentNullException.ThrowIfNull(mgr);
        ArgumentNullException.ThrowIfNull(env);
        if (!mgr.NeedsElevation || env.IsWindows)
            return (mgr.InstallFileName, mgr.InstallArgs);

        if (env.IsPrivileged)
            return (mgr.InstallFileName, mgr.InstallArgs);

        if (env.CommandExists("sudo"))
        {
            var sudoArgs = nonInteractive
                ? $"-n {mgr.InstallFileName} {mgr.InstallArgs}"
                : $"{mgr.InstallFileName} {mgr.InstallArgs}";
            return ("sudo", sudoArgs);
        }

        return null;
    }

    public static bool IsPrivileged() => new DefaultInstallEnvironment().IsPrivileged;

    public static async Task<(bool Ok, string Message)> InstallDepsAsync(
        bool installAria2,
        bool nonInteractive,
        CancellationToken ct)
    {
        var installer = CreateDefault();
        var lines = new List<string>
        {
            $"platform: {DescribeOs()}",
            $"tools: {AppPaths.ToolsBinDirectory}",
            $"privileged: {installer._env.IsPrivileged}",
        };
        var (netOk, netDetail) = await installer._network.CheckAsync(ct);
        lines.Add($"network: {(netOk ? "ok" : "fail")} ({netDetail})");

        var ok = true;
        if (installAria2)
        {
            try
            {
                var existing = await installer._locator.LocateAsync(ct);
                lines.Add($"aria2c: already installed ({existing})");
            }
            catch (FileNotFoundException)
            {
                lines.Add($"aria2c: installing {Aria2Manifest.Version}…");
                using var log = new StringWriter();
                var (installed, msg, kind) = await installer.RunInstallAria2Async(
                    autoYes: true, nonInteractive: nonInteractive, ct: ct, log: log);
                lines.Add($"aria2c: {msg} ({kind})");
                var logText = log.ToString().TrimEnd();
                if (logText.Length > 0)
                    lines.Add(logText);
                ok = installed;
            }
        }

        lines.Add("managers:");
        foreach (var mgr in installer._managers.Where(m => m.IsOsMatch()))
        {
            try
            {
                var probe = await installer._processes.RunAsync(mgr.ProbeFileName, mgr.ProbeArgs, ct);
                var avail = PackageManagerCatalog.IsPackageAvailable(mgr.ProbeKind, probe.ExitCode, probe.StdOut, probe.StdErr);
                lines.Add($"  {mgr.DisplayName}: {(avail ? "package available" : "not available")}");
            }
            catch (Exception ex) when (ex is InvalidOperationException or IOException or System.ComponentModel.Win32Exception)
            {
                lines.Add($"  {mgr.DisplayName}: probe failed");
            }
        }

        return (ok, string.Join(Environment.NewLine, lines));
    }

    private static async Task WriteNonEmptyAsync(TextWriter? log, string text)
    {
        if (log is null || string.IsNullOrWhiteSpace(text))
            return;
        await log.WriteLineAsync(text.TrimEnd());
    }

    private static string DescribeOs()
    {
        if (OperatingSystem.IsWindows()) return "Windows";
        if (OperatingSystem.IsMacOS()) return "macOS";
        if (OperatingSystem.IsLinux()) return "Linux";
        return "unknown";
    }

    private static void TryDeleteTree(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
