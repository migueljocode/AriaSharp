namespace AriaSharp.App;

/// <summary>Resolve output / session paths for a download run.</summary>
internal static class DownloadPaths
{
    public static (string OutputDir, string SessionPath) Resolve(AppSettings settings)
    {
        if (settings.SessionFile is not null)
        {
            var result = SessionLocator.Locate(settings.SessionFile, Directory.GetCurrentDirectory(), settings.OutputDir);
            return (result.OutputDir, result.SessionFile);
        }

        var outputDir = string.IsNullOrWhiteSpace(settings.OutputDir)
            ? AppDefaults.OutputDirDefault
            : settings.OutputDir;

        var sessionPath = Path.Join(outputDir, AppDefaults.SessionFileName);

        return (outputDir, sessionPath);
    }
}
