namespace AriaSharp.App;

/// <summary>Resolve output / session paths for a download run.</summary>
internal static class DownloadPaths
{
    public static (string OutputDir, string SessionPath) Resolve(AppSettings settings)
    {
        var outputDir = string.IsNullOrWhiteSpace(settings.OutputDir)
            ? AppDefaults.OutputDirDefault
            : settings.OutputDir;

        var sessionPath = string.IsNullOrWhiteSpace(settings.SessionFile)
            ? Path.Join(outputDir, AppDefaults.SessionFileName)
            : settings.SessionFile;

        return (outputDir, sessionPath);
    }
}
