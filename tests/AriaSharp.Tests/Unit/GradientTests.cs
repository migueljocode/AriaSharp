using System.Text.RegularExpressions;

using AriaSharp.App;
using Xunit;

namespace AriaSharp.Tests;

public partial class GradientTests
{
    [GeneratedRegex(@"\[[^\]]*\]")]
    private static partial Regex Tags();

    private static string Plain(string markup) => Tags().Replace(markup, string.Empty);

    [Theory]
    [InlineData(20, 0.0, -1)]
    [InlineData(20, 0.5, -1)]
    [InlineData(20, 1.0, -1)]
    [InlineData(20, 0.37, 5)]
    [InlineData(1, 0.5, 3)]
    [InlineData(80, double.NaN, 0)]
    [InlineData(20, 7.0, 2)]
    public void Bar_IsAlwaysExactlyWidthCells(int width, double fraction, int frame) =>
        Assert.Equal(width, Plain(Gradient.Bar(width, fraction, Gradient.Violet, frame)).Length);

    [Fact]
    public void Bar_EmptyAndFull_UseTrackAndBlocks()
    {
        Assert.Equal(new string('░', 10), Plain(Gradient.Bar(10, 0, Gradient.Violet)));
        Assert.Equal(new string('█', 10), Plain(Gradient.Bar(10, 1, Gradient.Violet)));
    }

    [Fact]
    public void Bar_PartialCell_UsesEighthBlock() =>
        Assert.Equal("█▌░░░░░░░░", Plain(Gradient.Bar(10, 0.15, Gradient.Violet)));

    [Fact]
    public void Bar_ZeroWidth_IsEmpty() => Assert.Equal(string.Empty, Gradient.Bar(0, 0.5, Gradient.Violet));

    [Fact]
    public void Sample_EndpointsMatchStops()
    {
        Assert.Equal(Gradient.Violet[0], Gradient.Sample(Gradient.Violet, 0));
        Assert.Equal(Gradient.Violet[^1], Gradient.Sample(Gradient.Violet, 1));
    }

    [Fact]
    public void Glide_ApproachesTargetWithoutOvershoot()
    {
        var shown = 0.0;
        for (var i = 0; i < 100; i++) shown = Gradient.Glide(shown, 0.8, 0.08);
        Assert.True(shown is > 0.79 and <= 0.8);
        Assert.Equal(0.3, Gradient.Glide(0.3, 0.3, 0.08));
    }

    [Fact]
    public void Text_Reveal_BlanksTheRightSide()
    {
        Assert.Equal("██████████", Plain(Gradient.Text("██████████", Gradient.Violet, 0, 1)));
        var half = Plain(Gradient.Text("██████████", Gradient.Violet, 0, 0.5));
        Assert.Equal(10, half.Length);
        Assert.True(half.StartsWith('█') && half.EndsWith(' '));
    }
}
