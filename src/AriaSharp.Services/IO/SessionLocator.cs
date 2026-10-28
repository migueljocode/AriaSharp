namespace AriaSharp.Services;

/// <summary>Resolves the session file and output directory for -s / --session.</summary>
public static class SessionLocator
{
    public record Result(string SessionFile, string Directory, string OutputDir);

    public static Result Locate(string? sessionValue, string cwd, string? explicitOutputDir)
    {
        string dir;
        string sessionFile;

        if (sessionValue is { Length: > 0 })
        {
            if (Directory.Exists(sessionValue))
            {
                dir = sessionValue;
                sessionFile = Path.Join(dir, AppDefaults.SessionFileName);
            }
            else if (File.Exists(sessionValue))
            {
                sessionFile = sessionValue;
                dir = Path.GetDirectoryName(sessionValue) ?? cwd;
            }
            else
            {
                dir = cwd;
                sessionFile = Path.Join(dir, AppDefaults.SessionFileName);
            }
        }
        else
        {
            dir = cwd;
            sessionFile = Path.Join(dir, AppDefaults.SessionFileName);
        }

        if (!File.Exists(sessionFile))
            throw new SessionNotFoundException($"no resume.session in {dir}");

        var outputDir = explicitOutputDir ?? dir;
        return new Result(sessionFile, dir, outputDir);
    }
}

public class SessionNotFoundException : Exception
{
    public SessionNotFoundException() { }
    public SessionNotFoundException(string message) : base(message) { }
    public SessionNotFoundException(string message, Exception? inner) : base(message, inner) { }
}
