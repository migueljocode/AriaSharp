using AriaSharp.Services;
using Xunit;

namespace AriaSharp.Tests;

public class InputResolverTests
{
    [Fact]
    public async Task Resolve_UrlArg_Returned()
    {
        var result = await InputResolver.ResolveAsync(
            ["https://example.com/a"],
            new UrlExtractor(),
            new UrlClassifier(),
            new FakeClipboard(null),
            forceClipboard: false,
            forceFiles: null,
            CancellationToken.None);
        Assert.Single(result);
        Assert.Equal("https://example.com/a", result[0]);
    }

    [Fact]
    public async Task Resolve_FileArg_ExtractsUrls()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "see https://example.com/one\nhttps://example.com/two\n");
            var result = await InputResolver.ResolveAsync(
                [path],
                new UrlExtractor(),
                new UrlClassifier(),
                new FakeClipboard(null),
                forceClipboard: false,
                forceFiles: null,
                CancellationToken.None);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, u => u.Contains("one", StringComparison.Ordinal));
            Assert.Contains(result, u => u.Contains("two", StringComparison.Ordinal));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ForceClipboard_IgnoresCliUrls()
    {
        var result = await InputResolver.ResolveAsync(
            ["https://example.com/ignored"],
            new UrlExtractor(),
            new UrlClassifier(),
            new FakeClipboard("https://clip.example/file"),
            forceClipboard: true,
            forceFiles: null,
            CancellationToken.None);
        Assert.Single(result);
        Assert.Contains("clip.example", result[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task ForceFile_Missing_Throws()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            InputResolver.ResolveAsync(
                [],
                new UrlExtractor(),
                new UrlClassifier(),
                new FakeClipboard(null),
                forceClipboard: false,
                forceFiles: ["/no/such/list.txt"],
                CancellationToken.None));
    }

    [Fact]
    public void Clipboard_ExtractsUrls()
    {
        var urls = new UrlExtractor().Extract("see https://example.com/x and magnet:?xt=urn:btih:abc123");
        Assert.Contains(urls, u => u.StartsWith("https://", StringComparison.Ordinal));
        Assert.Contains(urls, u => u.StartsWith("magnet:?", StringComparison.Ordinal));
    }

    private sealed class FakeClipboard(string? text) : IClipboardReader
    {
        public Task<string?> ReadTextAsync(CancellationToken ct) => Task.FromResult(text);
    }
}
