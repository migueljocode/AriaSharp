using System.Text.Json;

using AriaSharp.Models;
using AriaSharp.Services;

namespace AriaSharp.App;

/// <summary>
/// Composition root only: parse → settings → dispatch help / version / install / download.
/// </summary>
internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        AppSettings settings;
        SettingsOverlay cli;
        try
        {
            (settings, cli) = await SettingsBootstrap.LoadAsync(args).ConfigureAwait(false);
        }
        catch (CliException ex)
        {
            await System.Console.Error.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
            return (int)ExitCode.UsageError;
        }

        if (cli.Help == true)
        {
            Theme.Enabled = !(settings.NoColor || settings.Ascii || settings.Plain);
            HelpView.Render(settings);
            return (int)ExitCode.Ok;
        }

        if (cli.Version == true)
        {
            await System.Console.Out.WriteLineAsync("AriaSharp 1.0.0").ConfigureAwait(false);
            return (int)ExitCode.Ok;
        }

        if (cli.PrintConfig == true)
        {
            var json = JsonSerializer.Serialize(settings, AppSettingsJsonContext.Default.AppSettings);
            await System.Console.Out.WriteLineAsync(json).ConfigureAwait(false);
            return (int)ExitCode.Ok;
        }

        var wantsInstall = !settings.NoInstall
            && (settings.InstallDeps || settings.InstallAria2);
        if (wantsInstall && settings.Inputs.Count == 0 && !settings.FromClipboard && settings.FromFiles.Count == 0)
            return await InstallCommand.RunAsync(settings).ConfigureAwait(false);

        var (exit, resolved) = await InputCommand.ResolveAsync(settings).ConfigureAwait(false);
        if (exit is int code)
            return code;
        settings = resolved;

        while (true)
        {
            var (runCode, retry) = await DownloadCommand.RunAsync(settings).ConfigureAwait(false);
            if (!retry)
                return runCode;
        }
    }
}
