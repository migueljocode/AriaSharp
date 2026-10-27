using System.ComponentModel;

namespace AriaSharp.Services;

/// <summary>
/// Runs a child process and always drains stdout/stderr (avoids pipe-full hangs).
/// </summary>
internal static class ProcessRunner
{
    public static async Task<ProcessRunResult> RunAsync(
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
                return new ProcessRunResult(-1, "", "failed to start process");

            var outTask = process.StandardOutput.ReadToEndAsync(ct);
            var errTask = process.StandardError.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            return new ProcessRunResult(
                process.ExitCode,
                await outTask,
                await errTask);
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            return new ProcessRunResult(-1, "", ex.Message);
        }
    }
}
