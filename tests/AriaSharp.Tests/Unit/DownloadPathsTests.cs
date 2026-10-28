namespace AriaSharp.Tests;

public class DownloadPathsTests
{
    [Fact]
    public void Resolve_DefaultOutputDir_DoesNotCreateDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ariasharp-test-{Guid.NewGuid():N}");
        try
        {
            Assert.False(Directory.Exists(dir));
            var (outputDir, sessionPath) = DownloadPaths.Resolve(new AppSettings { OutputDir = dir });
            Assert.Equal(dir, outputDir);
            Assert.Equal(Path.Join(dir, "resume.session"), sessionPath);
            Assert.False(Directory.Exists(dir), "DownloadPaths.Resolve must not create the directory");
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir);
        }
    }

    [Fact]
    public void Resolve_ExplicitOutputDir_DoesNotCreateDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ariasharp-test-{Guid.NewGuid():N}");
        try
        {
            Assert.False(Directory.Exists(dir));
            var (outputDir, sessionPath) = DownloadPaths.Resolve(new AppSettings { OutputDir = dir });
            Assert.Equal(dir, outputDir);
            Assert.False(Directory.Exists(dir), "DownloadPaths.Resolve must not create the directory");
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir);
        }
    }

    [Fact]
    public void Resolve_DefaultOutput_ReturnsDot()
    {
        var (outputDir, sessionPath) = DownloadPaths.Resolve(new AppSettings());
        Assert.Equal(AppDefaults.OutputDirDefault, outputDir);
        Assert.Equal(Path.Join(AppDefaults.OutputDirDefault, AppDefaults.SessionFileName), sessionPath);
    }
}
