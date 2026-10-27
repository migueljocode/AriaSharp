using System.ComponentModel;

namespace AriaSharp.Services;

/// <summary>
/// Runs a child process and always drains stdout/stderr (avoids pipe-full hangs).
/// </summary>
internal static class ProcessRunner
{
    internal sealed record Result(int ExitCode, string StdOut, string StdErr);

    public static async Task<Result> RunAsync(
        string fileName,
        string arguments,
        CancellationToken ct,
        string? workingDirectory = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
        };

        try
        {
            using var process = Process.Start(psi);
            if (process is null)
                return new Result(-1, "", "failed to start process");

            var stdout = new StringBuilder();
            var stderr = new StringBuilder();
            var outTask = process.StandardOutput.ReadToEndAsync(ct);
            var errTask = process.StandardError.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct).ConfigureAwait(false);
            stdout.Append(await outTask.ConfigureAwait(false));
            stderr.Append(await errTask.ConfigureAwait(false));
            return new Result(process.ExitCode, stdout.ToString(), stderr.ToString());
        }
        catch (Win32Exception ex)
        {
            return new Result(-1, "", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return new Result(-1, "", ex.Message);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
    }
}
