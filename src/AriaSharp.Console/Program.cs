using System.Text;

namespace AriaSharp.Console;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var settings = ArgParser.Parse(args);
        if (settings.Help)
        {
            var sb = new StringBuilder();
            HelpDocument.Render(sb, 80);
            System.Console.WriteLine(sb.ToString());
            return (int)Models.ExitCode.UsageError;
        }
        if (settings.Version)
        {
            System.Console.WriteLine("AriaSharp 1.0.0");
            return (int)Models.ExitCode.Ok;
        }
        if (settings.Inputs.Length == 0)
        {
            System.Console.WriteLine("No inputs provided. Use --help for usage.");
            return (int)Models.ExitCode.UsageError;
        }
        try
        {
            var engine = new Services.Aria2Engine(new System.Net.Http.HttpClient());
            var store = new Services.SessionStore(settings.OutputDir ?? ".", settings.SessionFile ?? "resume.session");
            var calculator = new Services.ProgressCalculator();
            var session = new Services.DownloadSession(engine, store, calculator, settings);
            await session.RunAsync(CancellationToken.None).ConfigureAwait(false);
            return (int)Models.ExitCode.Ok;
        }
        catch (Exception ex)
        {
            System.Console.Error.WriteLine($"Error: {ex.Message}");
            return (int)Models.ExitCode.Failure;
        }
    }
}