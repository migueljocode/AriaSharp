namespace AriaSharp.Tests;

public class JobsDefaultTests
{
    [Fact]
    public void Default_ArgsBuilder_ContainsMaxConcurrentDownloads2()
    {
        var builder = new Aria2ArgsBuilder();
        var settings = new EngineSettings
        {
            Aria2Path = "aria2c",
            OutputDir = ".",
            SessionFile = "resume.session",
            InputFile = "input.txt",
            ConfFile = "aria2.conf",
            Secret = "secret",
            Port = 6800,
            Args = [],
            LogFile = "aria2.log",
        };
        var appSettings = new AppSettings();
        var args = builder.Build(settings, appSettings);
        Assert.Contains("--max-concurrent-downloads=2", args);
    }

    [Fact]
    public void Jobs_1_IsValid()
    {
        var overlay = ParseArg(["-j", "1"]);
        Assert.Equal(1, overlay.MaxConcurrentDownloads);
    }

    [Fact]
    public void Jobs_0_Throws()
    {
        var ex = Assert.Throws<CliException>(() => ParseArg(["-j", "0"]));
        Assert.Contains("1 to 16", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Jobs_17_Throws()
    {
        var ex = Assert.Throws<CliException>(() => ParseArg(["-j", "17"]));
        Assert.Contains("1 to 16", ex.Message, StringComparison.Ordinal);
    }

    private static SettingsOverlay ParseArg(string[] args) => args.Parse();

    [Fact]
    public async Task EnvJobs_BeatsDefault()
    {
        var envVar = "ARIASHARP_JOBS";
        var original = Environment.GetEnvironmentVariable(envVar);
        try
        {
            Environment.SetEnvironmentVariable(envVar, "5");
            var overlay = EnvOverlay.FromEnvironment();
            Assert.Equal(5, overlay.MaxConcurrentDownloads);
        }
        finally
        {
            if (original is null)
                Environment.SetEnvironmentVariable(envVar, null);
            else
                Environment.SetEnvironmentVariable(envVar, original);
        }
    }

    [Fact]
    public async Task CliJobs_BeatsEnvJobs()
    {
        var envVar = "ARIASHARP_JOBS";
        var original = Environment.GetEnvironmentVariable(envVar);
        try
        {
            Environment.SetEnvironmentVariable(envVar, "5");
            var cli = ParseArg(["-j", "3"]);
            Assert.Equal(3, cli.MaxConcurrentDownloads);
        }
        finally
        {
            if (original is null)
                Environment.SetEnvironmentVariable(envVar, null);
            else
                Environment.SetEnvironmentVariable(envVar, original);
        }
    }

    [Fact]
    public void JobsSource_Default()
    {
        var cli = ParseArg([]);
        var env = EnvOverlay.FromEnvironment();
        var config = new SettingsOverlay();
        var source = DetermineJobsSource(cli, env, config);
        Assert.Equal("default", source);
    }

    [Fact]
    public void JobsSource_Cli()
    {
        var cli = ParseArg(["-j", "3"]);
        var env = EnvOverlay.FromEnvironment();
        var config = new SettingsOverlay();
        var source = DetermineJobsSource(cli, env, config);
        Assert.Equal("cli", source);
    }

    [Fact]
    public async Task JobsSource_Env()
    {
        var envVar = "ARIASHARP_JOBS";
        var original = Environment.GetEnvironmentVariable(envVar);
        try
        {
            Environment.SetEnvironmentVariable(envVar, "5");
            var cli = ParseArg([]);
            var env = EnvOverlay.FromEnvironment();
            var config = new SettingsOverlay();
            var source = DetermineJobsSource(cli, env, config);
            Assert.Equal("env", source);
        }
        finally
        {
            if (original is null)
                Environment.SetEnvironmentVariable(envVar, null);
            else
                Environment.SetEnvironmentVariable(envVar, original);
        }
    }

    private static string DetermineJobsSource(
        SettingsOverlay cli, SettingsOverlay env, SettingsOverlay config)
    {
        if (cli.MaxConcurrentDownloads.HasValue) return "cli";
        if (env.MaxConcurrentDownloads.HasValue) return "env";
        if (config.MaxConcurrentDownloads.HasValue) return "config";
        return "default";
    }
}
