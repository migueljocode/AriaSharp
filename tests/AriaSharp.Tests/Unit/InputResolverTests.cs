
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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("just some text, no link here")]
    public async Task Fallback_NoClipboardUrl_UsesSessionFile(string? clip)
    {
        var session = await WriteSessionAsync("https://example.com/resume\n dir=/tmp/out\n");
        try
        {
            var result = await ResolveFallbackAsync(clip, session);
            Assert.Equal("https://example.com/resume", Assert.Single(result));
        }
        finally
        {
            File.Delete(session);
        }
    }

    [Fact]
    public async Task Fallback_ClipboardUrl_BeatsSessionFile()
    {
        var session = await WriteSessionAsync("https://example.com/resume\n");
        try
        {
            var result = await ResolveFallbackAsync("https://clip.example/file", session);
            Assert.Equal("https://clip.example/file", Assert.Single(result));
        }
        finally
        {
            File.Delete(session);
        }
    }

    [Fact]
    public async Task Fallback_ExplicitArg_BeatsSessionFile()
    {
        var session = await WriteSessionAsync("https://example.com/resume\n");
        try
        {
            var result = await InputResolver.ResolveAsync(
                ["https://example.com/arg"],
                new UrlExtractor(),
                new UrlClassifier(),
                new FakeClipboard(null),
                forceClipboard: false,
                forceFiles: null,
                CancellationToken.None,
                allowClipboardFallback: true,
                sessionFallbackPath: session);
            Assert.Equal("https://example.com/arg", Assert.Single(result));
        }
        finally
        {
            File.Delete(session);
        }
    }

    [Fact]
    public async Task Fallback_SessionMissingOrWithoutEntries_ReturnsEmpty()
    {
        var missing = Path.Join(Path.GetTempPath(), $"{Guid.NewGuid():N}.session");
        Assert.Empty(await ResolveFallbackAsync(null, missing));

        var empty = await WriteSessionAsync("\n# nothing\n");
        try
        {
            Assert.Empty(await ResolveFallbackAsync(null, empty));
        }
        finally
        {
            File.Delete(empty);
        }
    }

    [Fact]
    public async Task Fallback_Disabled_WhenPathNull()
        => Assert.Empty(await ResolveFallbackAsync(null, null));

    private static async Task<string> WriteSessionAsync(string content)
    {
        var path = Path.GetTempFileName();
        await File.WriteAllTextAsync(path, content);
        return path;
    }

    private static Task<IReadOnlyList<string>> ResolveFallbackAsync(string? clip, string? sessionPath)
        => InputResolver.ResolveAsync(
            [],
            new UrlExtractor(),
            new UrlClassifier(),
            new FakeClipboard(clip),
            forceClipboard: false,
            forceFiles: null,
            CancellationToken.None,
            allowClipboardFallback: true,
            sessionFallbackPath: sessionPath);

    private sealed class FakeClipboard(string? text) : IClipboardReader
    {
        public Task<string?> ReadTextAsync(CancellationToken ct) => Task.FromResult(text);
    }
}
