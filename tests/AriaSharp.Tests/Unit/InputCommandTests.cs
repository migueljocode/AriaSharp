namespace AriaSharp.Tests;

public class InputCommandTests
{
    [Fact]
    public async Task InvalidArgs_ReturnsUsageError_RejectedMessage()
    {
        var settings = new AppSettings { Inputs = ["not-a-url", "also-not-a-file"] };
        var (exit, resolved) = await InputCommand.ResolveAsync(settings);
        Assert.Equal((int)ExitCode.UsageError, exit);
        Assert.Empty(resolved.Inputs);
    }

    [Fact]
    public async Task ValidUrl_ReturnsNoExit()
    {
        var settings = new AppSettings { Inputs = ["https://example.com/file.zip"] };
        var (exit, resolved) = await InputCommand.ResolveAsync(settings);
        Assert.Null(exit);
        Assert.Single(resolved.Inputs);
        Assert.Equal("https://example.com/file.zip", resolved.Inputs[0]);
    }

    private static string MakeDir(bool withSession)
    {
        var dir = Directory.CreateTempSubdirectory("ariasharp-input-").FullName;
        if (withSession)
            File.WriteAllText(Path.Join(dir, AppDefaults.SessionFileName), "https://example.com/a\n");
        return dir;
    }

    [Fact]
    public async Task Session_NotSet_IsNoop()
    {
        var settings = new AppSettings();
        Assert.Null(await InputCommand.ApplySessionAsync(settings, "/nowhere"));
        Assert.False(settings.Resume);
        Assert.Null(settings.SessionFile);
    }

    [Fact]
    public async Task BareS_FindsResumeSessionInCwd_AndImpliesResume()
    {
        var cwd = MakeDir(withSession: true);
        try
        {
            var settings = new AppSettings { SessionFile = string.Empty };
            Assert.Null(await InputCommand.ApplySessionAsync(settings, cwd));
            Assert.Equal(Path.Join(cwd, AppDefaults.SessionFileName), settings.SessionFile);
            Assert.Equal(cwd, settings.OutputDir);
            Assert.True(settings.Resume);
        }
        finally
        {
            Directory.Delete(cwd, recursive: true);
        }
    }

    [Fact]
    public async Task BareS_WithoutResumeSession_ReturnsUsageError()
    {
        var cwd = MakeDir(withSession: false);
        try
        {
            var settings = new AppSettings { SessionFile = string.Empty };
            Assert.Equal((int)ExitCode.UsageError, await InputCommand.ApplySessionAsync(settings, cwd));
        }
        finally
        {
            Directory.Delete(cwd, recursive: true);
        }
    }

    [Fact]
    public async Task S_NonexistentPath_ReturnsUsageError_NoFallbackToCwd()
    {
        var cwd = MakeDir(withSession: true);
        try
        {
            var settings = new AppSettings { SessionFile = Path.Join(cwd, "does-not-exist") };
            Assert.Equal((int)ExitCode.UsageError, await InputCommand.ApplySessionAsync(settings, cwd));
            Assert.False(settings.Resume);
        }
        finally
        {
            Directory.Delete(cwd, recursive: true);
        }
    }

    [Fact]
    public async Task S_DirWithoutResumeSession_ReturnsUsageError()
    {
        var dir = MakeDir(withSession: false);
        try
        {
            var settings = new AppSettings { SessionFile = dir };
            Assert.Equal((int)ExitCode.UsageError, await InputCommand.ApplySessionAsync(settings, "/unused"));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task S_Dir_SetsOutputDirToThatDir_WhenNoExplicitOutput()
    {
        var dir = MakeDir(withSession: true);
        try
        {
            var settings = new AppSettings { SessionFile = dir };
            Assert.Null(await InputCommand.ApplySessionAsync(settings, "/unused"));
            Assert.Equal(dir, settings.OutputDir);
            Assert.Equal(Path.Join(dir, AppDefaults.SessionFileName), settings.SessionFile);
            Assert.True(settings.Resume);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task S_Dir_ExplicitOutputWins()
    {
        var dir = MakeDir(withSession: true);
        try
        {
            var settings = new AppSettings { SessionFile = dir, OutputDir = "/explicit/out" };
            Assert.Null(await InputCommand.ApplySessionAsync(settings, "/unused"));
            Assert.Equal("/explicit/out", settings.OutputDir);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task ResolveAsync_BadSession_ExitsWithUsageError_BeforeAnyInputWork()
    {
        var settings = new AppSettings
        {
            SessionFile = Path.Join(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}"),
            Inputs = ["https://example.com/a"],
        };
        var (exit, _) = await InputCommand.ResolveAsync(settings);
        Assert.Equal((int)ExitCode.UsageError, exit);
    }
}
