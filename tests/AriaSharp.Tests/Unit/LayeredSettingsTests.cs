
namespace AriaSharp.Tests;

public class LayeredSettingsTests
{
    private static readonly SettingsMerger Merger = new();
    private static AppSettings Defaults => Merger.Merge(new AppSettings());

    private static AppSettings Merge(params SettingsOverlay[] layers) => Merger.Merge(new AppSettings(), layers);

    [Fact]
    public void Cli_Overrides_Config_Overrides_Defaults()
    {
        var config = new SettingsOverlay { MaxConcurrentDownloads = 2, OutputDir = "/tmp/cfg" };
        var cli = new SettingsOverlay { MaxConcurrentDownloads = 8 };
        var settings = Merge(config, cli);
        Assert.Equal(8, settings.MaxConcurrentDownloads);
        Assert.Equal("/tmp/cfg", settings.OutputDir);
    }

    [Fact]
    public void NullFields_FallBackToBaseline()
    {
        var cli = new SettingsOverlay { Verbose = true };
        var settings = Merge(cli);
        Assert.True(settings.Verbose);
        Assert.Equal(AppDefaults.MaxConcurrentDownloads, settings.MaxConcurrentDownloads);
    }

    [Fact]
    public void LaterLayer_Wins()
    {
        var a = new SettingsOverlay { MaxConcurrentDownloads = 3 };
        var b = new SettingsOverlay { MaxConcurrentDownloads = 7 };
        var settings = Merge(a, b);
        Assert.Equal(7, settings.MaxConcurrentDownloads);
    }

    [Fact]
    public void Bool_ResetToFalse_ViaFalse()
    {
        var cli = new SettingsOverlay { Verbose = false };
        var settings = Merger.Merge(new AppSettings(), cli);
        Assert.False(settings.Verbose);
    }

    [Fact]
    public void Integer_ResetToDefault_ByOmittingField()
    {
        // Rebuilding the default baseline and omitting the field in every layer leaves the
        // baseline default in place — that is the reset-to-default contract.
        var settings = Merge(new SettingsOverlay { MaxConcurrentDownloads = null });
        Assert.Equal(AppDefaults.MaxConcurrentDownloads, settings.MaxConcurrentDownloads);
    }

    [Fact]
    public void Inputs_AccumulateAcrossLayers()
    {
        var cli = new SettingsOverlay { Inputs = ["https://a"] };
        var settings = Merge(cli);
        Assert.Single(settings.Inputs);
        Assert.Equal("https://a", settings.Inputs[0]);
    }

    [Fact]
    public void OptionTable_Has_Exactly_The_Spec_Options()
    {
        var shortNames = OptionTable.All.Where(o => o.ShortName.HasValue).Select(o => o.ShortName!.Value).Order().ToArray();
        Assert.Equal(['V', 'c', 'f', 'h', 'i', 'j', 'k', 'l', 'n', 'o', 's', 'v', 'y'], shortNames);
    }

    [Fact]
    public void OptionTable_Categories_Grouped()
    {
        Assert.Contains("Input & Output", OptionTable.Categories);
        Assert.Contains("Meta", OptionTable.Categories);
    }

    [Fact]
    public void HelpDocument_Renders_From_OptionTable()
    {
        var help = HelpDocument.Render(Defaults);
        Assert.Contains("--help", help, StringComparison.Ordinal);
        Assert.Contains("--version", help, StringComparison.Ordinal);
        Assert.Contains("-j", help, StringComparison.Ordinal);
        Assert.Contains("min-split-size", help, StringComparison.Ordinal);
        Assert.Contains("USAGE", help, StringComparison.Ordinal);
    }
}
