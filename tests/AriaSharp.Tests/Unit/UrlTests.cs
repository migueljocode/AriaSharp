using AriaSharp.Services;
using Xunit;

namespace AriaSharp.Tests;

public class UrlTests
{
    [Fact]
    public void Extractor_FindsHttpAndHttps()
    {
        var extractor = new UrlExtractor();
        var urls = extractor.Extract("see https://example.com/a and http://ex.org/b");
        Assert.Equal(2, urls.Length);
        Assert.Contains("https://example.com/a", urls);
        Assert.Contains("http://ex.org/b", urls);
    }

    [Fact]
    public void Extractor_FindsMagnetWithoutSlashSlash()
    {
        var extractor = new UrlExtractor();
        const string magnet = "magnet:?xt=urn:btih:0123456789abcdef0123456789abcdef01234567&dn=test";
        var urls = extractor.Extract($"try {magnet} please");
        Assert.Single(urls);
        Assert.Equal(magnet, urls[0]);
    }

    [Fact]
    public void Extractor_StripsTrailingPunctuation()
    {
        var extractor = new UrlExtractor();
        var urls = extractor.Extract("link: https://example.com/file.zip.");
        Assert.Single(urls);
        Assert.Equal("https://example.com/file.zip", urls[0]);
    }

    [Fact]
    public void Classifier_RecognisesMagnet()
    {
        var classifier = new UrlClassifier();
        Assert.True(classifier.IsUrl("magnet:?xt=urn:btih:abc"));
        Assert.True(classifier.IsUrl("https://example.com"));
        Assert.False(classifier.IsUrl("not-a-url"));
    }

    [Fact]
    public void Sanitize_PreservesQueryAmpersandsAndQuotes()
    {
        var sanitizer = new UrlSanitizer();
        const string url = "https://example.com/path?a=1&b=2&q=\"hello\"";
        var cleaned = sanitizer.Sanitize(url);
        Assert.Equal(url, cleaned);
    }

    [Fact]
    public void Sanitize_ReplacesControlAndMarkupBrackets()
    {
        var sanitizer = new UrlSanitizer();
        var cleaned = sanitizer.Sanitize("https://ex.com/[bad]\x01");
        Assert.Equal("https://ex.com/?bad??", cleaned);
        Assert.DoesNotContain("\x01", cleaned, StringComparison.Ordinal);
        Assert.DoesNotContain("[", cleaned, StringComparison.Ordinal);
    }

    [Fact]
    public void MaskCredentials_HidesUserPass()
    {
        var sanitizer = new UrlSanitizer();
        var masked = sanitizer.MaskCredentials("https://user:secret@example.com/file");
        Assert.Equal("https://***@example.com/file", masked);
        Assert.DoesNotContain("secret", masked, StringComparison.Ordinal);
    }
}
