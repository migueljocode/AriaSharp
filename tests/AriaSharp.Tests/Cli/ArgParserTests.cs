using Xunit;
using AriaSharp.App;
using AriaSharp.Models;

namespace AriaSharp.Tests;

public class ArgParserTests
{
    [Fact]
    public void Help_SetsHelpFlag()
    {
        var settings = ArgParser.Parse(["-h"]);
        Assert.True(settings.Help == true);
        settings = ArgParser.Parse(["--help"]);
        Assert.True(settings.Help == true);
    }

    [Fact]
    public void Version_SetsVersionFlag()
    {
        var settings = ArgParser.Parse(["-V"]);
        Assert.True(settings.Version == true);
        settings = ArgParser.Parse(["--version"]);
        Assert.True(settings.Version == true);
    }

    [Fact]
    public void Input_Repeatable()
    {
        var settings = ArgParser.Parse(["-i", "https://a/file", "--input", "https://b/file", "https://c/file"]);
        Assert.Equal(3, settings.Inputs.Count);
        Assert.Contains("https://a/file", settings.Inputs);
        Assert.Contains("https://b/file", settings.Inputs);
        Assert.Contains("https://c/file", settings.Inputs);
    }

    [Fact]
    public void Output_MapsToOutputDir()
    {
        var settings = ArgParser.Parse(["-o", "/tmp/out"]);
        Assert.Equal("/tmp/out", settings.OutputDir);
        settings = ArgParser.Parse(["--output", "/tmp/out2"]);
        Assert.Equal("/tmp/out2", settings.OutputDir);
    }

    [Fact]
    public void Force_IsFlagNotValue()
    {
        var settings = ArgParser.Parse(["-f", "https://example.com/file"]);
        Assert.True(settings.Force == true);
        Assert.Single(settings.Inputs);
        Assert.Equal("https://example.com/file", settings.Inputs[0]);
    }

    [Fact]
    public void Jobs_ValidatedRange()
    {
        var settings = ArgParser.Parse(["-j", "4"]);
        Assert.True(settings.MaxConcurrentDownloads == 4);
        var ex = Assert.Throws<CliException>(() => ArgParser.Parse(["-j", "0"]));
        Assert.Contains("1 to 16", ex.Message, StringComparison.Ordinal);
        ex = Assert.Throws<CliException>(() => ArgParser.Parse(["-j", "17"]));
        Assert.Contains("1 to 16", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Connections_ValidatedRange()
    {
        var settings = ArgParser.Parse(["-c", "8"]);
        Assert.True(settings.Connections == 8);
        var ex = Assert.Throws<CliException>(() => ArgParser.Parse(["-c", "100"]));
        Assert.Contains("1 to 16", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Limit_Set()
    {
        var settings = ArgParser.Parse(["-l", "2M"]);
        Assert.Equal("2M", settings.SpeedLimit);
    }

    [Fact]
    public void Session_Set()
    {
        var settings = ArgParser.Parse(["-s", "/tmp/resume.session"]);
        Assert.Equal("/tmp/resume.session", settings.SessionFile);
    }

    [Fact]
    public void InstallDeps_NoInstall()
    {
        var settings = ArgParser.Parse(["-y"]);
        Assert.True(settings.InstallDeps == true);
        settings = ArgParser.Parse(["-n"]);
        Assert.True(settings.NoInstall == true);
    }

    [Fact]
    public void Verbose_Set()
    {
        var settings = ArgParser.Parse(["-v"]);
        Assert.True(settings.Verbose == true);
    }

    [Fact]
    public void LongOpt_EqualsForm()
    {
        var settings = ArgParser.Parse(["--jobs=5", "--connections=4"]);
        Assert.True(settings.MaxConcurrentDownloads == 5);
        Assert.True(settings.Connections == 4);
    }

    [Fact]
    public void LongOpt_SpaceForm()
    {
        var settings = ArgParser.Parse(["--jobs", "5"]);
        Assert.True(settings.MaxConcurrentDownloads == 5);
    }

    [Fact]
    public void DashDash_TerminatesOptions()
    {
        var settings = ArgParser.Parse(["--", "https://example.com/file", "-h"]);
        Assert.Equal(2, settings.Inputs.Count);
        Assert.Equal("https://example.com/file", settings.Inputs[0]);
        Assert.Equal("-h", settings.Inputs[1]);
        Assert.True(settings.Help == null || settings.Help == false);
    }

    [Fact]
    public void UnknownOption_Throws()
    {
        var ex = Assert.Throws<CliException>(() => ArgParser.Parse(["--bogus"]));
        Assert.Contains("--bogus", ex.Message, StringComparison.Ordinal);
        Assert.Contains("--help", ex.Message, StringComparison.Ordinal);
        ex = Assert.Throws<CliException>(() => ArgParser.Parse(["-z"]));
        Assert.Contains("-z", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MissingValue_Throws()
    {
        var ex = Assert.Throws<CliException>(() => ArgParser.Parse(["-j"]));
        Assert.Contains("Missing value", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ShortCluster_Flags()
    {
        var settings = ArgParser.Parse(["-fkv"]);
        Assert.True(settings.Force == true);
        Assert.True(settings.KeepListFiles == true);
        Assert.True(settings.Verbose == true);
    }

    [Fact]
    public void ShortCluster_WithValue()
    {
        var settings = ArgParser.Parse(["-j3"]);
        Assert.True(settings.MaxConcurrentDownloads == 3);
    }

    [Fact]
    public void NoColor_LongOnly()
    {
        var settings = ArgParser.Parse(["--no-color"]);
        Assert.True(settings.NoColor == true);
    }

    [Fact]
    public async Task Program_Help_ExitsZero()
    {
        var exitCode = await Program.Main(["-h"]);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Program_MissingInputs_ReturnsUsageError()
    {
        var exitCode = await Program.Main(Array.Empty<string>());
        Assert.Equal(2, exitCode);
    }

    [Fact]
    public async Task Program_Inputs_ReturnsNotImplemented()
    {
        var oldError = System.Console.Error;
        using var sw = new System.IO.StringWriter();
        System.Console.SetError(sw);
        try
        {
            var exitCode = await Program.Main(["https://example.com/file"]);
            Assert.Equal(1, exitCode);
            Assert.Contains("engine not implemented yet", sw.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            System.Console.SetError(oldError);
        }
    }
}
