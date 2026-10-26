using AriaSharp.Models;

namespace AriaSharp.Services;

/// <summary>
/// Installs aria2 and optional clipboard helpers via the host package manager.
/// Source/tarball builds are intentionally not used here (hang risk + no sudo).
/// </summary>
public sealed class PrerequisiteInstaller : IPrerequisiteInstaller
{
    public async Task<bool> InstallAsync(string package, CancellationToken ct)
    {
        var pkg = package switch
        {
            "wl-clipboard" => "wl-clipboard",
            "xclip" => "xclip",
            "xsel" => "xsel",
            "aria2" or "aria2c" => "aria2",
            _ => package,
        };
        return await InstallPackageAsync(pkg, ct).ConfigureAwait(false);
    }

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
        var result = await ProcessRunner.RunAsync(WhichCommand(), command, ct).ConfigureAwait(false);
        return result.ExitCode == 0;
    }

    public static async Task<bool> InstallAria2Async(CancellationToken ct) =>
        await InstallPackageAsync("aria2", ct).ConfigureAwait(false);

    public static async Task<(bool Ok, string Message)> InstallDepsAsync(bool installAria2, CancellationToken ct)
    {
        var messages = new List<string>();
        var ok = true;

        if (installAria2)
        {
            var already = await new PrerequisiteInstaller().IsInstalledAsync("aria2c", ct).ConfigureAwait(false);
            if (already)
            {
                messages.Add("aria2c: already installed");
            }
            else
            {
                var installed = await InstallPackageAsync("aria2", ct).ConfigureAwait(false);
                if (installed)
                    messages.Add("aria2: installed");
                else
                {
                    messages.Add("aria2: install failed (need package manager + privileges)");
                    ok = false;
                }
            }
        }

        foreach (var pkg in new[] { "wl-clipboard", "xclip", "xsel" })
        {
            var installer = new PrerequisiteInstaller();
            var present = await installer.IsInstalledAsync(pkg, ct).ConfigureAwait(false);
            if (present)
            {
                messages.Add($"{pkg}: already installed");
                continue;
            }
            // Best-effort; clipboard helpers are optional
            var installed = await installer.InstallAsync(pkg, ct).ConfigureAwait(false);
            messages.Add(installed ? $"{pkg}: installed" : $"{pkg}: skipped/failed");
        }

        return (ok, string.Join(Environment.NewLine, messages));
    }

    private static async Task<bool> InstallPackageAsync(string package, CancellationToken ct)
    {
        if (OperatingSystem.IsWindows())
        {
            // winget is preferred when available; silent fail if not
            var winget = await ProcessRunner.RunAsync("winget", $"install -e --id aria2.aria2 --accept-package-agreements --accept-source-agreements", ct).ConfigureAwait(false);
            if (package == "aria2" || package == "aria2c")
                return winget.ExitCode == 0;
            return false;
        }

        if (OperatingSystem.IsMacOS())
        {
            var brew = await ProcessRunner.RunAsync("brew", $"install {package}", ct).ConfigureAwait(false);
            return brew.ExitCode == 0;
        }

        // Linux: try apt-get, then dnf, then pacman
        foreach (var (cmd, args) in new (string, string)[]
                 {
                     ("apt-get", $"install -y {package}"),
                     ("dnf", $"install -y {package}"),
                     ("pacman", $"-S --noconfirm {package}"),
                 })
        {
            var which = await ProcessRunner.RunAsync(WhichCommand(), cmd, ct).ConfigureAwait(false);
            if (which.ExitCode != 0) continue;
            var result = await ProcessRunner.RunAsync(cmd, args, ct).ConfigureAwait(false);
            if (result.ExitCode == 0) return true;
        }
        return false;
    }

    private static string WhichCommand() => OperatingSystem.IsWindows() ? "where" : "which";
}
