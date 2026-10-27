namespace AriaSharp.Services;

/// <summary>
/// Owner-only aria2 RPC conf (secret never on the command line).
/// </summary>
internal static class Aria2ConfWriter
{
    public static async Task WriteAsync(string path, string secret, int port, CancellationToken ct)
    {
        var options = new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.Write,
            Share = FileShare.None,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
        };
        if (!OperatingSystem.IsWindows())
            options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;

        var stream = new FileStream(path, options);
        await using (stream)
        {
            var writer = new StreamWriter(stream) { NewLine = "\n" };
            await using (writer)
            {
                await writer.WriteLineAsync($"rpc-secret={secret}");
                await writer.WriteLineAsync("enable-rpc=true");
                await writer.WriteLineAsync("rpc-listen-all=false");
                await writer.WriteLineAsync($"rpc-listen-port={port}");
                await writer.FlushAsync(ct);
            }
        }

        await RestrictToCurrentUserAsync(path, ct);
    }

    private static async Task RestrictToCurrentUserAsync(string path, CancellationToken ct)
    {
        if (!OperatingSystem.IsWindows())
            return;
        try
        {
            var user = Environment.UserName;
            var args = string.Concat("\"", path, "\" /inheritance:r /grant:r \"", user, ":F\"");
            _ = await ProcessRunner.RunAsync("icacls", args, ct);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or UnauthorizedAccessException)
        {
            // Best-effort; loopback RPC still limits exposure.
        }
    }
}
