using System.Text;
using AriaSharp.Models;
using AriaSharp.Services;

namespace AriaSharp.App;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        AppSettings settings;
        try
        {
            settings = ArgParser.Parse(args);
        }
        catch (CliException ex)
        {
            await System.Console.Error.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
            return (int)Models.ExitCode.UsageError;
        }

        if (settings.Help)
        {
            var sb = new StringBuilder();
            HelpDocument.Render(sb, 80);
            await System.Console.Out.WriteLineAsync(sb.ToString()).ConfigureAwait(false);
            return (int)Models.ExitCode.Ok;
        }
        if (settings.Version)
        {
            await System.Console.Out.WriteLineAsync("AriaSharp 1.0.0").ConfigureAwait(false);
            return (int)Models.ExitCode.Ok;
        }
        if (settings.Inputs.Count == 0)
        {
            await System.Console.Out.WriteLineAsync("No inputs provided. Use --help for usage.").ConfigureAwait(false);
            return (int)Models.ExitCode.UsageError;
        }
        await System.Console.Error.WriteLineAsync("engine not implemented yet").ConfigureAwait(false);
        return (int)Models.ExitCode.Failure;
    }
}