using System.Globalization;
using System.Text;

using AriaSharp.Models;

namespace AriaSharp.Services;

public interface ISessionStore
{
    string OutputDir { get; }
    string SessionFile { get; }
    string TempInputFile { get; }
    string TempConfFile { get; }
    string LogFile { get; }
    Task<string> PrepareInputAsync(string[] inputs, string outputDir, CancellationToken ct);
    Task WriteConfFileAsync(string secret, CancellationToken ct);
    Task<int> CountSessionEntriesAsync(CancellationToken ct);
    Task DeleteSessionAsync(CancellationToken ct);
    Task<bool> SessionHasEntriesAsync(CancellationToken ct);
    Task AppendLogAsync(string message, CancellationToken ct);
}

public sealed class SessionStore : ISessionStore
{
    private readonly string _outputDir, _sessionFile, _tempInputFile, _tempConfFile, _logFile;
    public SessionStore(string outputDir, string sessionFile)
    {
        _outputDir = outputDir; _sessionFile = sessionFile;
        _tempInputFile = Path.Combine(Path.GetTempPath(), $"ariasharp-input-{Guid.NewGuid():N}.txt");
        _tempConfFile = Path.Combine(Path.GetTempPath(), $"ariasharp-conf-{Guid.NewGuid():N}.conf");
        _logFile = Path.Combine(Path.GetTempPath(), $"ariasharp-log-{Guid.NewGuid():N}.log");
    }
    public string OutputDir => _outputDir;
    public string SessionFile => _sessionFile;
    public string TempInputFile => _tempInputFile;
    public string TempConfFile => _tempConfFile;
    public string LogFile => _logFile;

    public async Task<string> PrepareInputAsync(string[] inputs, string outputDir, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        var sb = new StringBuilder();
        foreach (var input in inputs)
        {
            if (string.IsNullOrEmpty(input)) continue;
            sb.AppendLine(input.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal));
        }
        await File.WriteAllTextAsync(_tempInputFile, sb.ToString(), ct).ConfigureAwait(false);
        return _tempInputFile;
    }
    public async Task WriteConfFileAsync(string secret, CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"rpc-secret={secret}");
        await File.WriteAllTextAsync(_tempConfFile, sb.ToString(), ct).ConfigureAwait(false);
    }
    public async Task<int> CountSessionEntriesAsync(CancellationToken ct)
    {
        if (!File.Exists(_sessionFile)) return 0;
        var count = 0;
        await foreach (var line in File.ReadLinesAsync(_sessionFile, ct).ConfigureAwait(false))
            if (!string.IsNullOrWhiteSpace(line)) count++;
        return count;
    }
    public Task DeleteSessionAsync(CancellationToken ct) { if (File.Exists(_sessionFile)) File.Delete(_sessionFile); return Task.CompletedTask; }
    public async Task<bool> SessionHasEntriesAsync(CancellationToken ct)
    {
        if (!File.Exists(_sessionFile)) return false;
        await foreach (var line in File.ReadLinesAsync(_sessionFile, ct).ConfigureAwait(false))
            if (!string.IsNullOrWhiteSpace(line)) return true;
        return false;
    }
    public async Task AppendLogAsync(string message, CancellationToken ct)
    {
        try { await File.AppendAllTextAsync(_logFile, $"{DateTime.UtcNow:u} {message}{Environment.NewLine}", ct).ConfigureAwait(false); }
        catch (IOException) { }
    }
}