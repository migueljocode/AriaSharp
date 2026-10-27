using System.IO;

namespace AriaSharp.Tests;

public class FileIoTests : IAsyncDisposable
{
    private readonly List<string> _dirsToDelete = new();

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        foreach (var dir in _dirsToDelete)
        {
            try { Directory.Delete(dir, recursive: true); } catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private string MakeTempDir()
    {
        var dir = Directory.CreateTempSubdirectory("ariasharp-test-");
        _dirsToDelete.Add(dir.FullName);
        return dir.FullName;
    }

    [Fact]
    public async Task RunWorkspace_ExposesJoinedPaths()
    {
        var dir = MakeTempDir();
        var ws = await RunWorkspace.CreateAsync();
        Assert.Equal(Path.Join(ws.Root.FullName, "input.txt"), ws.InputFile);
        Assert.Equal(Path.Join(ws.Root.FullName, "aria2.conf"), ws.ConfFile);
        Assert.Equal(Path.Join(ws.Root.FullName, "aria2.log"), ws.LogFile);
        await ws.DisposeAsync();
    }

    [Fact]
    public async Task RunWorkspace_Dispose_DeletesDirectory()
    {
        var dir = Directory.CreateTempSubdirectory("ariasharp-dispose-").FullName;
        var ws = new RunWorkspace(new DirectoryInfo(dir));
        await File.WriteAllTextAsync(Path.Join(dir, "input.txt"), string.Empty);
        await ws.DisposeAsync();
        Assert.False(Directory.Exists(dir));
    }

    [Fact]
    public async Task InputMerger_WritesLines_WithLfOnly()
    {
        var dir = MakeTempDir();
        var dest = Path.Join(dir, "input.txt");
        await InputMerger.WriteAsync(["https://a/file", "https://b/file", "", "https://c/file"], dest);
        var text = await File.ReadAllTextAsync(dest);
        Assert.DoesNotContain("\r", text, StringComparison.Ordinal);
        Assert.Contains("https://a/file\nhttps://b/file\nhttps://c/file\n", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InputMerger_Handles100kLines_WithoutWholeFileBuffering()
    {
        var dir = MakeTempDir();
        var dest = Path.Join(dir, "input.txt");
        var lines = new string[100_000];
        for (var i = 0; i < lines.Length; i++) lines[i] = $"https://example.com/file{i}";
        await InputMerger.WriteAsync(lines, dest);
        var written = await File.ReadAllLinesAsync(dest);
        var count = written.Length;
        Assert.Equal(100_000, count);
    }

    [Fact]
    public void SessionFile_IsAria2ListFile_CRLF_URLLine()
    {
        var dir = MakeTempDir();
        var dest = Path.Join(dir, "list.txt");
        File.WriteAllText(dest, "http://example.com/file1\r\nhttps://example.com/file2\r\n");
        Assert.True(SessionFile.IsAria2ListFile(dest));
    }

    [Fact]
    public void SessionFile_IsAria2ListFile_RecognisedOptionKey()
    {
        var dir = MakeTempDir();
        var dest = Path.Join(dir, "list.txt");
        File.WriteAllText(dest, "dir=/downloads\r\nout=file.rar\r\n");
        Assert.True(SessionFile.IsAria2ListFile(dest));
    }

    [Fact]
    public void SessionFile_IsAria2ListFile_PlainEqualsIsNotEnough()
    {
        var dir = MakeTempDir();
        var dest = Path.Join(dir, "list.txt");
        File.WriteAllText(dest, "some-random-key=value\r\n");
        Assert.False(SessionFile.IsAria2ListFile(dest));
    }

    [Fact]
    public async Task SessionFile_ConcurrentReadWhileWriting()
    {
        var dir = MakeTempDir();
        var dest = Path.Join(dir, "session.session");
        using var cts = new CancellationTokenSource();
        var writeTask = Task.Run(async () =>
        {
            using var fs = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
            using var writer = new StreamWriter(fs);
            for (var i = 0; i < 50; i++)
            {
                await writer.WriteLineAsync($"http://example.com/file{i}").ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);
                await Task.Delay(5).ConfigureAwait(false);
            }
        }, cts.Token);
        var readTask = Task.Run(async () =>
        {
            await Task.Delay(10).ConfigureAwait(false);
            var found = false;
            await foreach (var _ in SessionFile.EnumerateEntriesAsync(dest, cts.Token).ConfigureAwait(false)) { found = true; }
            Assert.True(found);
        }, cts.Token);
        await Task.WhenAll(writeTask, readTask);
    }

    [Fact]
    public void SessionFile_HasEntries_ShortCircuits()
    {
        var dir = MakeTempDir();
        var dest = Path.Join(dir, "session.session");
        File.WriteAllText(dest, "http://example.com/file1\nhttp://example.com/file2\n");
        Assert.True(SessionFile.HasEntries(dest));
        File.WriteAllText(dest, "\n\n");
        Assert.False(SessionFile.HasEntries(dest));
        Assert.False(SessionFile.HasEntries(Path.Join(dir, "missing.session")));
    }

    [Fact]
    public async Task SettingsStore_CorruptFile_FallsBackToDefaults()
    {
        var dir = MakeTempDir();
        var path = Path.Join(dir, "settings.json");
        await File.WriteAllTextAsync(path, "{ not valid json }");
        var store = new SettingsStore();
        var settings = await store.LoadOverlayAsync(path, CancellationToken.None);
        Assert.Null(settings.MaxConcurrentDownloads);
        Assert.Null(settings.Split);
    }

    [Fact]
    public async Task SettingsStore_AtomicWrite_NoTempFileLeftBehind()
    {
        var dir = MakeTempDir();
        var path = Path.Join(dir, "settings.json");
        var store = new SettingsStore();
        await store.SaveOverlayAsync(new SettingsOverlay { MaxConcurrentDownloads = 5 }, path, CancellationToken.None);
        Assert.True(File.Exists(path));
        Assert.Empty(Directory.GetFiles(dir, "settings-*.tmp"));
        var loaded = await store.LoadOverlayAsync(path, CancellationToken.None);
        Assert.True(loaded.MaxConcurrentDownloads == 5);
    }

    [Fact]
    public void UrlExtractor_CRLFInput_TrimsCorrectly()
    {
        var extractor = new UrlExtractor();
        var result = extractor.Extract("https://a/file\r\nhttps://b/file,\r\nhttps://c/file?");
        Assert.Equal(["https://a/file", "https://b/file", "https://c/file"], result);
    }
}
