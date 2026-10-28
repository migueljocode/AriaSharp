using System.IO;

namespace AriaSharp.Services;

public static class InputMerger
{
    private const FileOptions StreamOptions = FileOptions.Asynchronous | FileOptions.SequentialScan;

    /// <summary>
    /// Writes <paramref name="inputs"/> one per line, then (when <paramref name="sessionFile"/> is
    /// given and exists) stream-copies the session file after them. aria2 accepts one --input-file,
    /// so resume entries and new URLs share the destination.
    /// </summary>
    public static async Task WriteAsync(
        string[] inputs,
        string destination,
        string? sessionFile = null,
        CancellationToken ct = default)
    {
        ThrowIfNull(inputs);
        ThrowIfNull(destination);
        using var stream = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, StreamOptions);
        using (var writer = new StreamWriter(stream, leaveOpen: true) { NewLine = "\n" })
        {
            foreach (var input in inputs)
            {
                ct.ThrowIfCancellationRequested();
                if (string.IsNullOrEmpty(input)) continue;
                await writer.WriteLineAsync(input);
            }
            await writer.FlushAsync(ct);
        }

        if (sessionFile is { Length: > 0 } && File.Exists(sessionFile))
        {
            using var source = new FileStream(sessionFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 4096, StreamOptions);
            await source.CopyToAsync(stream, ct);
        }

        await stream.FlushAsync(ct);
    }
}
