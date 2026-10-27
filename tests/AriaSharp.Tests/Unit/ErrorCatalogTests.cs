
namespace AriaSharp.Tests;

public class ErrorCatalogTests
{
    [Fact]
    public void TextFor_ReturnsCorrectTextForAllCodes()
    {
        var expected = new Dictionary<int, string>
        {
            [1] = "Unknown error",
            [2] = "Timeout",
            [3] = "Resource not found",
            [4] = "Max file not found",
            [5] = "Download too slow",
            [6] = "Network problem",
            [7] = "Unfinished",
            [8] = "Cannot resume",
            [9] = "Not enough disk space",
            [10] = "Piece length changed",
            [11] = "Duplicate download",
            [12] = "Duplicate info hash",
            [13] = "File already exists",
            [14] = "File renaming failed",
            [15] = "File open error",
            [16] = "File create error",
            [17] = "File I/O error",
            [18] = "Directory create error",
            [19] = "Name resolution failed",
            [20] = "Metalink parse error",
            [21] = "FTP protocol error",
            [22] = "HTTP protocol error",
            [23] = "Too many redirects",
            [24] = "HTTP authentication failed",
            [25] = "Bencode parse error",
            [26] = "BitTorrent parse error",
            [27] = "Magnet parse error",
            [28] = "Option error",
            [29] = "HTTP service unavailable",
            [30] = "JSON parse error",
            [31] = "Removed by user",
            [32] = "Checksum error",
        };
        foreach (var (code, text) in expected)
        {
            var actual = ErrorCatalog.TextFor(code);
            Assert.True(actual == text, $"Code {code}: expected '{text}', actual '{actual}'");
        }
    }

    [Fact]
    public void TextFor_UnknownCode_ReturnsDefault()
    {
        Assert.Contains("Unknown error", ErrorCatalog.TextFor(0), StringComparison.Ordinal);
        Assert.Contains("Unknown error", ErrorCatalog.TextFor(100), StringComparison.Ordinal);
        Assert.Contains("Unknown error", ErrorCatalog.TextFor(-1), StringComparison.Ordinal);
    }

    [Fact]
    public void HintFor_ReturnsNonEmptyForAllCodes()
    {
        for (var code = 1; code <= 32; code++)
        {
            Assert.False(string.IsNullOrWhiteSpace(ErrorCatalog.HintFor(code)), $"Code {code} hint is empty");
        }
    }

    [Fact]
    public void Classify_Network()
    {
        Assert.Equal(Aria2ErrorKind.Network, ErrorCatalog.Classify(6));
        Assert.Equal(Aria2ErrorKind.Network, ErrorCatalog.Classify(19));
    }

    [Fact]
    public void Classify_Http()
    {
        Assert.Equal(Aria2ErrorKind.Http, ErrorCatalog.Classify(3));
        Assert.Equal(Aria2ErrorKind.Http, ErrorCatalog.Classify(23));
        Assert.Equal(Aria2ErrorKind.Http, ErrorCatalog.Classify(24));
        Assert.Equal(Aria2ErrorKind.Http, ErrorCatalog.Classify(29));
    }

    [Fact]
    public void Classify_Disk()
    {
        Assert.Equal(Aria2ErrorKind.Disk, ErrorCatalog.Classify(9));
    }

    [Fact]
    public void Classify_File()
    {
        Assert.Equal(Aria2ErrorKind.File, ErrorCatalog.Classify(1));
        Assert.Equal(Aria2ErrorKind.File, ErrorCatalog.Classify(13));
        Assert.Equal(Aria2ErrorKind.File, ErrorCatalog.Classify(32));
    }

    [Fact]
    public void Classify_Protocol()
    {
        Assert.Equal(Aria2ErrorKind.Protocol, ErrorCatalog.Classify(21));
        Assert.Equal(Aria2ErrorKind.Protocol, ErrorCatalog.Classify(22));
        Assert.Equal(Aria2ErrorKind.Protocol, ErrorCatalog.Classify(27));
    }

    [Fact]
    public void Classify_Unknown()
    {
        Assert.Equal(Aria2ErrorKind.Unknown, ErrorCatalog.Classify(5));
        Assert.Equal(Aria2ErrorKind.Unknown, ErrorCatalog.Classify(11));
        Assert.Equal(Aria2ErrorKind.Unknown, ErrorCatalog.Classify(28));
        Assert.Equal(Aria2ErrorKind.Unknown, ErrorCatalog.Classify(999));
    }
}
