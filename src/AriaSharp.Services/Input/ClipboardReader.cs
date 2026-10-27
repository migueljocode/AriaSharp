namespace AriaSharp.Services;

/// <summary>
/// Cross-platform clipboard text reader (process tools only — AOT-safe).
/// Linux: wl-paste → xclip → xsel. macOS: pbpaste. Windows: PowerShell Get-Clipboard.
/// </summary>
public interface IClipboardReader
{
    Task<string?> ReadTextAsync(CancellationToken ct);
}

public sealed class ClipboardReader : IClipboardReader
{
    private static readonly (string Cmd, string Args)[] LinuxTools =
    [
        ("wl-paste", "--no-newline"),
        ("xclip", "-selection clipboard -o"),
        ("xsel", "--clipboard --output"),
    ];

    public async Task<string?> ReadTextAsync(CancellationToken ct)
    {
        if (OperatingSystem.IsWindows())
        {
            var r = await ProcessRunner.RunAsync(
                    "powershell",
                    "-NoProfile -Command Get-Clipboard",
                    ct)
                .ConfigureAwait(false);
            return r.ExitCode == 0 ? NullIfEmpty(r.StdOut) : null;
        }

        if (OperatingSystem.IsMacOS())
        {
            var r = await ProcessRunner.RunAsync("pbpaste", "", ct).ConfigureAwait(false);
            return r.ExitCode == 0 ? NullIfEmpty(r.StdOut) : null;
        }

        foreach (var (cmd, args) in LinuxTools)
        {
            var which = await ProcessRunner.RunAsync("which", cmd, ct).ConfigureAwait(false);
            if (which.ExitCode != 0)
                continue;
            var r = await ProcessRunner.RunAsync(cmd, args, ct).ConfigureAwait(false);
            if (r.ExitCode == 0 && r.StdOut is { Length: > 0 })
                return NullIfEmpty(r.StdOut);
        }

        return null;
    }

    private static string? NullIfEmpty(string s) =>
        s.Trim() is { Length: > 0 } t ? t : null;
}
