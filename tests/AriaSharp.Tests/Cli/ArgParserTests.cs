
namespace AriaSharp.Tests;

public class ArgParserTests
{
    private static SettingsOverlay Parse(params string[] args) => args.Parse();

    [Fact]
    public void Help_SetsHelpFlag()
    {
        var settings = Parse(["-h"]);
        Assert.True(settings.Help == true);
        settings = Parse(["--help"]);
        Assert.True(settings.Help == true);
    }

    [Fact]
    public void Version_SetsVersionFlag()
    {
        var settings = Parse(["-V"]);
        Assert.True(settings.Version == true);
        settings = Parse(["--version"]);
        Assert.True(settings.Version == true);
    }

    [Fact]
    public void Input_Repeatable()
    {
        var settings = Parse(["-i", "https://a/file", "--input", "https://b/file", "https://c/file"]);
        Assert.Equal(3, settings.Inputs.Count);
        Assert.Contains("https://a/file", settings.Inputs);
        Assert.Contains("https://b/file", settings.Inputs);
        Assert.Contains("https://c/file", settings.Inputs);
    }

    [Fact]
    public void Output_MapsToOutputDir()
    {
        var settings = Parse(["-o", "/tmp/out"]);
        Assert.Equal("/tmp/out", settings.OutputDir);
        settings = Parse(["--output", "/tmp/out2"]);
        Assert.Equal("/tmp/out2", settings.OutputDir);
    }

    [Fact]
    public void Force_IsFlagNotValue()
    {
        var settings = Parse(["-f", "https://example.com/file"]);
        Assert.True(settings.Force == true);
        Assert.Single(settings.Inputs);
        Assert.Equal("https://example.com/file", settings.Inputs[0]);
    }

    [Fact]
    public void Jobs_ValidatedRange()
    {
        var settings = Parse(["-j", "4"]);
        Assert.True(settings.MaxConcurrentDownloads == 4);
        var ex = Assert.Throws<CliException>(() => Parse(["-j", "0"]));
        Assert.Contains("1 to 16", ex.Message, StringComparison.Ordinal);
        ex = Assert.Throws<CliException>(() => Parse(["-j", "17"]));
        Assert.Contains("1 to 16", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Connections_ValidatedRange()
    {
        var settings = Parse(["-c", "8"]);
        Assert.True(settings.Connections == 8);
        var ex = Assert.Throws<CliException>(() => Parse(["-c", "100"]));
        Assert.Contains("1 to 16", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Limit_Set()
    {
        var settings = Parse(["-l", "2M"]);
        Assert.Equal("2M", settings.SpeedLimit);
    }

    [Fact]
    public void Session_Set()
    {
        var settings = Parse(["-s", "/tmp/resume.session"]);
        Assert.Equal("/tmp/resume.session", settings.SessionFile);
    }

    [Fact]
    public void Yes_LongOnly()
    {
        var settings = Parse(["--yes"]);
        Assert.True(settings.Yes == true);
        Assert.Null(settings.InstallDeps);
    }

    [Fact]
    public void InstallDeps_ShortY_AndNoInstall_ShortN()
    {
        // dneat / Brief: -y == --install-deps
        var settings = Parse(["-y"]);
        Assert.True(settings.InstallDeps == true);
        settings = Parse(["--install-deps"]);
        Assert.True(settings.InstallDeps == true);
        settings = Parse(["-n"]);
        Assert.True(settings.NoInstall == true);
    }

    [Fact]
    public void Verbose_Set()
    {
        var settings = Parse(["-v"]);
        Assert.True(settings.Verbose == true);
    }

    [Fact]
    public void LongOpt_EqualsForm()
    {
        var settings = Parse(["--jobs=5", "--connections=4"]);
        Assert.True(settings.MaxConcurrentDownloads == 5);
        Assert.True(settings.Connections == 4);
    }

    [Fact]
    public void LongOpt_SpaceForm()
    {
        var settings = Parse(["--jobs", "5"]);
        Assert.True(settings.MaxConcurrentDownloads == 5);
    }

    [Fact]
    public void DashDash_TerminatesOptions()
    {
        var settings = Parse(["--", "https://example.com/file", "-h"]);
        Assert.Equal(2, settings.Inputs.Count);
        Assert.Equal("https://example.com/file", settings.Inputs[0]);
        Assert.Equal("-h", settings.Inputs[1]);
        Assert.True(settings.Help == null || settings.Help == false);
    }

    [Fact]
    public void UnknownOption_Throws()
    {
        var ex = Assert.Throws<CliException>(() => Parse(["--bogus"]));
        Assert.Contains("--bogus", ex.Message, StringComparison.Ordinal);
        Assert.Contains("--help", ex.Message, StringComparison.Ordinal);
        ex = Assert.Throws<CliException>(() => Parse(["-z"]));
        Assert.Contains("-z", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MissingValue_Throws()
    {
        var ex = Assert.Throws<CliException>(() => Parse(["-j"]));
        Assert.Contains("Missing value", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ShortCluster_Flags()
    {
        var settings = Parse(["-fkv"]);
        Assert.True(settings.Force == true);
        Assert.True(settings.KeepListFiles == true);
        Assert.True(settings.Verbose == true);
    }

    [Fact]
    public void ShortCluster_WithValue()
    {
        var settings = Parse(["-j3"]);
        Assert.True(settings.MaxConcurrentDownloads == 3);
    }

    [Fact]
    public void NoColor_LongOnly()
    {
        var settings = Parse(["--no-color"]);
        Assert.True(settings.NoColor == true);
    }

    [Fact]
    public async Task Program_Help_ExitsZero()
    {
        var exitCode = await AppHost.RunAsync(["-h"]);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Program_MissingInputs_ReturnsUsageError()
    {
        var exitCode = await AppHost.RunAsync(Array.Empty<string>());
        Assert.Equal(2, exitCode);
    }

    [Fact]
    public async Task Program_Inputs_MissingAria2_FailsHonestly()
    {
        var oldError = System.Console.Error;
        using var sw = new System.IO.StringWriter();
        System.Console.SetError(sw);
        try
        {
            var exitCode = await AppHost.RunAsync(["--aria2-path", "/nonexistent/aria2c", "https://example.com/file"]);
            Assert.Equal(1, exitCode);
            var err = sw.ToString();
            Assert.DoesNotContain("engine not implemented yet", err, StringComparison.Ordinal);
            Assert.Contains("aria2", err, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            System.Console.SetError(oldError);
        }
    }

    [Fact]
    public void AllShortFlags_ParseWithoutError()
    {
        // Every short letter currently claimed by OptionTable
        var settings = Parse(["-y", "-n", "-f", "-k", "-v", "-h"]);
        Assert.True(settings.InstallDeps == true);
        Assert.True(settings.NoInstall == true);
        Assert.True(settings.Force == true);
        Assert.True(settings.KeepListFiles == true);
        Assert.True(settings.Verbose == true);
        Assert.True(settings.Help == true);
    }

    [Fact]
    public void LongFlags_CoreSet()
    {
        var settings = Parse([
            "--plain", "--inline", "--no-color", "--ascii", "--no-pager",
            "--resume", "--force", "--auto-file-renaming",
            "--install-deps", "--install-aria2", "--print-config",
        ]);
        Assert.True(settings.Plain == true);
        Assert.True(settings.Inline == true);
        Assert.True(settings.NoColor == true);
        Assert.True(settings.Ascii == true);
        Assert.True(settings.NoPager == true);
        Assert.True(settings.Resume == true);
        Assert.True(settings.Force == true);
        Assert.True(settings.AutoFileRenaming == true);
        Assert.True(settings.InstallDeps == true);
        Assert.True(settings.InstallAria2 == true);
        Assert.True(settings.PrintConfig == true);
    }

    [Fact]
    public void ValueOptions_AttachedAndSeparated()
    {
        var a = Parse(["-o", "/tmp/a", "-j", "3", "-c", "8", "-l", "2M", "-s", "/tmp/s.session"]);
        Assert.Equal("/tmp/a", a.OutputDir);
        Assert.Equal(3, a.MaxConcurrentDownloads);
        Assert.Equal(8, a.Connections);
        Assert.Equal("2M", a.SpeedLimit);
        Assert.Equal("/tmp/s.session", a.SessionFile);

        var b = Parse(["-j3", "-c8", "--output=/tmp/b", "--limit=500K"]);
        Assert.Equal(3, b.MaxConcurrentDownloads);
        Assert.Equal(8, b.Connections);
        Assert.Equal("/tmp/b", b.OutputDir);
        Assert.Equal("500K", b.SpeedLimit);
    }
}
