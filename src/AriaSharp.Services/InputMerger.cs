using System.IO;

namespace AriaSharp.Services;

public static class InputMerger
{
    public static async Task WriteAsync(string[] inputs, string destination, CancellationToken ct = default)
    {
        ThrowIfNull(inputs);
        ThrowIfNull(destination);
        using var stream = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var writer = new StreamWriter(stream) { NewLine = "\n" };
        foreach (var input in inputs)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrEmpty(input)) continue;
            await writer.WriteLineAsync(input).ConfigureAwait(false);
        }
        await writer.FlushAsync(ct).ConfigureAwait(false);
    }
}
