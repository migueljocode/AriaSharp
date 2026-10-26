using Xunit;
using AriaSharp.Models;
using AriaSharp.Services;

namespace AriaSharp.Tests;

public class Aria2ArgsBuilderTests
{
    private static EngineSettings Settings(int port = 6800, string conf = "/tmp/conf", string input = "/tmp/input", string session = "/tmp/resume.session", string dir = "/tmp/out", string secret = "s3cr3t")
        => new()
        {
            Aria2Path = "/usr/bin/aria2c",
            Port = port,
            ConfFile = conf,
            InputFile = input,
            SessionFile = session,
            OutputDir = dir,
            Secret = secret,
            Args = Array.Empty<string>(),
        };

    private static string[] Build(AppSettings? appSettings = null, int port = 6800)
        => new Aria2ArgsBuilder().Build(Settings(port: port), appSettings ?? new AppSettings());

    [Fact]
    public void Build_UsesLowercaseBooleans()
    {
        var args = Build(new AppSettings { AutoFileRenaming = true, AllowOverwrite = false });
        Assert.Contains("--auto-file-renaming=true", args);
        Assert.Contains("--allow-overwrite=false", args);
        Assert.DoesNotContain("--auto-file-renaming=True", args);
        Assert.DoesNotContain("--allow-overwrite=False", args);
        Assert.DoesNotContain("--auto-file-renaming=TRUE", args);
        Assert.DoesNotContain("--allow-overwrite=FALSE", args);
    }

    [Fact]
    public void Build_NeverPutsSecretOnCommandLine()
    {
        var args = Build(new AppSettings(), port: 6800);
        foreach (var arg in args)
        {
            Assert.DoesNotContain("rpc-secret", arg, StringComparison.Ordinal);
            Assert.DoesNotContain("s3cr3t", arg, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Build_UsesConfPathForSecret()
    {
        var args = Build(new AppSettings());
        Assert.Contains("--conf-path=/tmp/conf", args);
    }

    [Fact]
    public void Build_HonoursSaveSession()
    {
        var withSession = Build(new AppSettings { SaveSession = true });
        Assert.Contains("--save-session=/tmp/resume.session", withSession);
        Assert.Contains("--save-session-interval=60", withSession);

        var withoutSession = Build(new AppSettings { SaveSession = false });
        Assert.DoesNotContain("--save-session=", withoutSession);
        Assert.DoesNotContain("--save-session-interval=", withoutSession);
    }

    [Fact]
    public void Build_ConvertsAria2OptToKeyEqualsValue()
    {
        var args = Build(new AppSettings { ExtraOptions = ["split=3", "max-connection-per-server=8"] });
        Assert.Contains("--split=3", args);
        Assert.Contains("--max-connection-per-server=8", args);
        Assert.DoesNotContain("--aria2-opt", args);
    }

    [Fact]
    public void Build_RejectsBlockedKeys()
    {
        var blocked = new[]
        {
            "rpc-secret=x", "enable-rpc=true", "conf-path=/x", "no-conf",
            "stop-with-process=1", "input-file=/x", "save-session=/x",
            "on-download-start=cmd", "ON-DOWNLOAD-COMPLETE=cmd", "rpc-user=u",
        };
        foreach (var opt in blocked)
        {
            var args = Build(new AppSettings { ExtraOptions = [opt] });
            Assert.DoesNotContain(opt, args);
            Assert.DoesNotContain("--" + opt.Split('=')[0] + "=", args);
        }
    }

    [Fact]
    public void Build_AllowsNonBlockedOpt()
    {
        var args = Build(new AppSettings { ExtraOptions = ["max-overall-download-limit=1M", "min-split-size=2M"] });
        Assert.Contains("--max-overall-download-limit=1M", args);
        Assert.Contains("--min-split-size=2M", args);
    }

    [Fact]
    public void Build_IgnoresMalformedOpt()
    {
        var args = Build(new AppSettings { ExtraOptions = ["novalue", "=", "=", "keyonly"] });
        Assert.DoesNotContain("novalue", args.SelectMany(a => a.Split('=')).ToArray(), StringComparer.Ordinal);
    }
}
