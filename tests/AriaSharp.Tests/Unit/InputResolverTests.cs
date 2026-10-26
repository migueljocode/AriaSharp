using AriaSharp.Services;
using Xunit;

namespace AriaSharp.Tests;

public class InputResolverTests
{
    [Fact]
    public async Task Resolve_ExistingInputs_ReturnedAsIs()
    {
        var result = await InputResolver.ResolveAsync(
            ["https://example.com/a"],
            new UrlExtractor(),
            new FakeClipboard(null),
            CancellationToken.None);
        Assert.Single(result);
        Assert.Equal("https://example.com/a", result[0]);
    }

    [Fact]
    public async Task Clipboard_ExtractsUrls()
    {
        // Direct extractor check used by clipboard path
        var urls = new UrlExtractor().Extract("see https://example.com/x and magnet:?xt=urn:btih:abc123");
        Assert.Contains(urls, u => u.StartsWith("https://", StringComparison.Ordinal));
        Assert.Contains(urls, u => u.StartsWith("magnet:?", StringComparison.Ordinal));
    }

    [Fact]
    public async Task FakeClipboard_ReadReturnsText()
    {
        var clip = new FakeClipboard("https://cdn.example/file.zip");
        var text = await clip.ReadTextAsync(CancellationToken.None);
        Assert.Equal("https://cdn.example/file.zip", text);
    }

    private sealed class FakeClipboard(string? text) : IClipboardReader
    {
        public Task<string?> ReadTextAsync(CancellationToken ct) => Task.FromResult(text);
    }
}
