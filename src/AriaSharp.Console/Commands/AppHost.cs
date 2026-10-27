namespace AriaSharp.App;

/// <summary>
/// Callable entry used by top-level <c>Program.cs</c> and integration tests.
/// </summary>
internal static class AppHost
{
    public static async Task<int> RunAsync(string[] args)
    {
        AppSettings settings;
        SettingsOverlay cli;
        try
        {
            (settings, cli) = await SettingsBootstrap.LoadAsync(args).ConfigureAwait(false);
        }
        catch (CliException ex)
        {
            await Console.Error.WriteLineAsync($"Error: {ex.Message}").ConfigureAwait(false);
            return (int)ExitCode.UsageError;
        }

        Theme.Enabled = !(settings.NoColor || settings.Ascii || settings.Plain);

        return cli switch
        {
            { Help: true } => RenderHelp(settings),
            { Version: true } => await WriteVersionAsync().ConfigureAwait(false),
            { PrintConfig: true } => await WriteConfigAsync(settings).ConfigureAwait(false),
            _ => await RunCoreAsync(settings).ConfigureAwait(false),
        };
    }

    private static int RenderHelp(AppSettings settings)
    {
        HelpView.Render(settings);
        return (int)ExitCode.Ok;
    }

    private static async Task<int> WriteVersionAsync()
    {
        await Console.Out.WriteLineAsync("AriaSharp 1.0.0").ConfigureAwait(false);
        return (int)ExitCode.Ok;
    }

    private static async Task<int> WriteConfigAsync(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, AppSettingsJsonContext.Default.AppSettings);
        await Console.Out.WriteLineAsync(json).ConfigureAwait(false);
        return (int)ExitCode.Ok;
    }

    private static async Task<int> RunCoreAsync(AppSettings settings)
    {
        var wantsInstall = !settings.NoInstall
            && (settings.InstallDeps || settings.InstallAria2);
        if (wantsInstall
            && settings.Inputs is { Count: 0 }
            && !settings.FromClipboard
            && settings.FromFiles is { Count: 0 })
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
