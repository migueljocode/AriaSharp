namespace AriaSharp.Tests;

public class SessionLocatorTests
{
    [Fact]
    public void SessionValue_IsExistingDirectory_UsesResumeSessionInDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ariasharp-session-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        var sessionFile = Path.Join(dir, "resume.session");
        File.WriteAllText(sessionFile, "");

        try
        {
            var result = SessionLocator.Locate(dir, "/cwd", null);
            Assert.Equal(sessionFile, result.SessionFile);
            Assert.Equal(dir, result.Directory);
            Assert.Equal(dir, result.OutputDir);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void SessionValue_IsExistingFile_UsesThatFile()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ariasharp-session-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        var sessionFile = Path.Join(dir, "my.session");
        File.WriteAllText(sessionFile, "");

        try
        {
            var result = SessionLocator.Locate(sessionFile, "/cwd", null);
            Assert.Equal(sessionFile, result.SessionFile);
            Assert.Equal(dir, result.Directory);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void SessionValue_Null_AutoUsesCwd()
    {
        var cwd = Path.Combine(Path.GetTempPath(), $"ariasharp-session-{Guid.NewGuid():N}");
        Directory.CreateDirectory(cwd);
        var sessionFile = Path.Join(cwd, "resume.session");
        File.WriteAllText(sessionFile, "");

        try
        {
            var result = SessionLocator.Locate(null, cwd, null);
            Assert.Equal(sessionFile, result.SessionFile);
            Assert.Equal(cwd, result.Directory);
            Assert.Equal(cwd, result.OutputDir);
        }
        finally
        {
            Directory.Delete(cwd, recursive: true);
        }
    }

    [Fact]
    public void SessionValue_DirMissing_ThrowsSessionNotFound()
    {
        var ex = Assert.Throws<SessionNotFoundException>(() =>
            SessionLocator.Locate("/nonexistent/dir", "/cwd", null));
        Assert.Contains("no resume.session in /cwd", ex.Message);
    }

    [Fact]
    public void ExplicitOutputDir_WinsOverSessionDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ariasharp-session-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        var sessionFile = Path.Join(dir, "resume.session");
        File.WriteAllText(sessionFile, "");

        var outDir = Path.Combine(Path.GetTempPath(), $"ariasharp-output-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outDir);

        try
        {
            var result = SessionLocator.Locate(dir, "/cwd", outDir);
            Assert.Equal(outDir, result.OutputDir);
            Assert.Equal(dir, result.Directory);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
            Directory.Delete(outDir, recursive: true);
        }
    }
}
