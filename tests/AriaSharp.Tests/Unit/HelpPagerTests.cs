using AriaSharp.App;
using Xunit;

namespace AriaSharp.Tests;

public class HelpPagerTests
{
    [Fact]
    public void Wrap_ShortLines_Unchanged()
    {
        var lines = HelpPager.Wrap("hello\nworld", 40);
        Assert.Equal(2, lines.Count);
        Assert.Equal("hello", lines[0]);
        Assert.Equal("world", lines[1]);
    }

    [Fact]
    public void Wrap_LongLine_BreaksAtWidth()
    {
        var longLine = new string('a', 100);
        var lines = HelpPager.Wrap(longLine, 40);
        Assert.True(lines.Count >= 3);
        Assert.All(lines, l => Assert.True(l.Length <= 40));
    }

    [Fact]
    public void HelpDocument_ContainsPagerKeysAndOptions()
    {
        var doc = HelpDocument.Render(new Models.AppSettings());
        Assert.Contains("--install-deps", doc, StringComparison.Ordinal);
        Assert.Contains("EXIT CODES", doc, StringComparison.Ordinal);
        Assert.Contains("help pager", doc, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("-y", doc, StringComparison.Ordinal);
    }

    [Fact]
    public void Show_NoPager_WritesFullDocument()
    {
        // Redirection is the normal test host; Show must not throw.
        HelpPager.Show("line1\nline2\n", noPager: true);
    }
}
