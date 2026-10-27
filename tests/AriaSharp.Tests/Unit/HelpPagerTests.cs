
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
        HelpPager.Show("line1\nline2\n", noPager: true);
    }

    [Fact]
    public void RenderAnsi_ContainsColorsAndSections()
    {
        var ansi = HelpView.RenderAnsi(new Models.AppSettings(), width: 100);
        Assert.False(string.IsNullOrWhiteSpace(ansi));
        Assert.Contains("\u001b[", ansi, StringComparison.Ordinal);
        Assert.Contains("USAGE", ansi, StringComparison.Ordinal);
        Assert.Contains("EXIT CODES", ansi, StringComparison.Ordinal);
        Assert.Contains("KEYS", ansi, StringComparison.Ordinal);
    }

    [Fact]
    public void SplitAnsiLines_PreservesRows()
    {
        var lines = HelpPager.SplitAnsiLines("a\nb\nc");
        Assert.Equal(3, lines.Count);
    }
}
