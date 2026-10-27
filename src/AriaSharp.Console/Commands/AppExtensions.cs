namespace AriaSharp.App;

/// <summary>C# 14 extension members for app entry and command dispatch.</summary>
internal static class AppExtensions
{
    extension(string[] args)
    {
        /// <summary>Run the full AriaSharp host (settings → help/install/download).</summary>
        public Task<int> RunAriaSharpAsync() => AppHost.RunAsync(args);
    }

    extension(AppSettings settings)
    {
        public Task<int> RunInstallAsync() => InstallCommand.RunAsync(settings);

        public Task<(int? ExitCode, AppSettings Settings)> ResolveInputsAsync() =>
            InputCommand.ResolveAsync(settings);

        public Task<(int Code, bool Retry)> RunDownloadAsync() =>
            DownloadCommand.RunAsync(settings);
    }
}
