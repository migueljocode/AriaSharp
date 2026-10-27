namespace AriaSharp.App;

/// <summary>C# 14 extension members for app entry and command dispatch.</summary>
internal static class AppExtensions
{
    extension(string[] args)
    {
        /// <summary>Run the full AriaSharp host (settings → help/install/download).</summary>
        public async Task<int> RunAriaSharpAsync()
        {
            AppSettings settings;
            SettingsOverlay cli;
            try
            {
                (settings, cli) = await SettingsBootstrap.LoadAsync(args);
            }
            catch (CliException ex)
            {
                await Console.Error.WriteLineAsync($"Error: {ex.Message}");
                return (int)ExitCode.UsageError;
            }

            Theme.Enabled = !(settings.NoColor || settings.Ascii || settings.Plain);

            if (cli.Help is true)
                return settings.RenderHelp();

            if (cli.Version is true)
            {
                // Single line identity string — no private helper needed.
                await Console.Out.WriteLineAsync("AriaSharp 1.0.0");
                return (int)ExitCode.Ok;
            }

            if (cli.PrintConfig is true)
                return await settings.WriteConfigAsync();

            return await settings.RunCoreAsync();
        }
    }

    extension(AppSettings settings)
    {
        public int RenderHelp()
        {
            settings.ShowHelp();
            return (int)ExitCode.Ok;
        }

        public async Task<int> WriteConfigAsync()
        {
            var json = JsonSerializer.Serialize(settings, AppSettingsJsonContext.Default.AppSettings);
            await Console.Out.WriteLineAsync(json);
            return (int)ExitCode.Ok;
        }

        public async Task<int> RunCoreAsync()
        {
            var wantsInstall = !settings.NoInstall
                && (settings.InstallDeps || settings.InstallAria2);
            if (wantsInstall
                && settings.Inputs is { Count: 0 }
                && !settings.FromClipboard
                && settings.FromFiles is { Count: 0 })
                return await settings.RunInstallAsync();

            var (exit, resolved) = await settings.ResolveInputsAsync();
            if (exit is int code)
                return code;

            settings = resolved;
            while (true)
            {
                var (runCode, retry) = await settings.RunDownloadAsync();
                if (!retry)
                    return runCode;
            }
        }

        public Task<int> RunInstallAsync() => InstallCommand.RunAsync(settings);

        public Task<(int? ExitCode, AppSettings Settings)> ResolveInputsAsync() =>
            InputCommand.ResolveAsync(settings);

        public Task<(int Code, bool Retry)> RunDownloadAsync() =>
            DownloadCommand.RunAsync(settings);
    }
}
