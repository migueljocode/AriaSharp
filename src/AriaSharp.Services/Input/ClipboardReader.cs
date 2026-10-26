namespace AriaSharp.Services;

/// <summary>
/// Cross-platform clipboard text reader (process tools only — AOT-safe, no unsafe).
/// Linux: wl-paste → xclip → xsel. macOS: pbpaste. Windows: powershell Get-Clipboard.
/// </summary>
public interface IClipboardReader
{
    Task<string?> ReadTextAsync(CancellationToken ct);
}

public sealed class ClipboardReader : IClipboardReader
{
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

        foreach (var (cmd, args) in new (string, string)[]
                 {
                     ("wl-paste", "--no-newline"),
                     ("xclip", "-selection clipboard -o"),
                     ("xsel", "--clipboard --output"),
                 })
        {
            var which = await ProcessRunner.RunAsync("which", cmd, ct).ConfigureAwait(false);
            if (which.ExitCode != 0) continue;
            var r = await ProcessRunner.RunAsync(cmd, args, ct).ConfigureAwait(false);
            if (r.ExitCode == 0 && !string.IsNullOrWhiteSpace(r.StdOut))
                return NullIfEmpty(r.StdOut);
        }
        return null;
    }

    private static string? NullIfEmpty(string s)
    {
        var t = s.Trim();
        return t.Length == 0 ? null : t;
    }
}
