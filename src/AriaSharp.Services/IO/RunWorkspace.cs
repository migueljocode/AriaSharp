using System.IO;

namespace AriaSharp.Services;

public sealed class RunWorkspace : IAsyncDisposable
{
    private bool _disposed;
    public DirectoryInfo Root { get; }
    public string InputFile => Path.Join(Root.FullName, "input.txt");
    public string ConfFile => Path.Join(Root.FullName, "aria2.conf");
    public string LogFile => Path.Join(Root.FullName, "aria2.log");

    public RunWorkspace(DirectoryInfo root) { Root = root; }

    public static async Task<RunWorkspace> CreateAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        // CreateTempSubdirectory is owner-only (0700) on Unix by default.
        var root = Directory.CreateTempSubdirectory("ariasharp-");
        return new RunWorkspace(root);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try { Directory.Delete(Root.FullName, recursive: true); }
        catch (DirectoryNotFoundException) { }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
