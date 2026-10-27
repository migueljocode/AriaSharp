
namespace AriaSharp.Tests;

public class MaskCredentialsTests
{
    [Theory]
    [InlineData("https://user:pass@example.com/file", "https://***@example.com/file")]
    [InlineData("ftp://user@host/a.bin", "ftp://***@host/a.bin")]
    [InlineData("https://example.com/path@name/file", "https://example.com/path@name/file")]
    [InlineData("https://example.com/f?mail=a@b.c", "https://example.com/f?mail=a@b.c")]
    [InlineData("no url here", "no url here")]
    public void MaskCredentials_MasksOnlyTheAuthority(string input, string expected) =>
        Assert.Equal(expected, MaskCredentials(input));
}
