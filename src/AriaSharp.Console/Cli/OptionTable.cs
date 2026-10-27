namespace AriaSharp.App;

internal enum OptionKind { Flag, Integer, String, List }

internal sealed class Option
{
    public Option(string longName, OptionKind kind, string category, string help,
                  Action<SettingsOverlay, string?> apply,
                  char? shortName = null, int? min = null, int? max = null,
                  string? valueName = null, string? defaultValue = null)
    {
        LongName = longName;
        Kind = kind;
        Category = category;
        Help = help;
        Apply = apply;
        ShortName = shortName;
        Min = min;
        Max = max;
        ValueName = valueName;
        DefaultValue = defaultValue;
    }

    public string LongName { get; }
    public char? ShortName { get; }
    public OptionKind Kind { get; }
    public int? Min { get; }
    public int? Max { get; }
    public bool TakesValue => Kind != OptionKind.Flag;
    public string Category { get; }
    public string Help { get; }
    public string? ValueName { get; }
    public string? DefaultValue { get; }
    public Action<SettingsOverlay, string?> Apply { get; }
}

internal static class OptionTable
{
    public static readonly Option[] All = Build();
    public static readonly string[] Categories = All.Select(o => o.Category).Distinct(StringComparer.Ordinal).ToArray();

    private static readonly Dictionary<string, Option> ByLong = All.ToDictionary(o => o.LongName, StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<char, Option> ByShort = All.Where(o => o.ShortName.HasValue).ToDictionary(o => o.ShortName!.Value);

    public static Option? FindByLong(string name) => ByLong.TryGetValue(name, out var o) ? o : null;
    public static Option? FindByShort(char c) => ByShort.TryGetValue(c, out var o) ? o : null;

    private static Option[] Build()
    {
        return new[]
        {
            // ── INPUT & OUTPUT
            new Option("input",     OptionKind.List,    "Input & Output", "Add a URL, magnet or list file (repeatable).",                                       apply: (o, v) => o.Inputs = o.Inputs.Append(v ?? string.Empty).ToArray(),                shortName: 'i'),
            new Option("from-clipboard", OptionKind.Flag, "Input & Output", "Force read URLs from the system clipboard only.", apply: (o, _) => o.FromClipboard = true),
            new Option("from-file", OptionKind.List, "Input & Output", "Force read URLs from a text/list file (repeatable).", valueName: "<path>", apply: (o, v) => o.FromFiles = o.FromFiles.Append(v ?? string.Empty).ToArray()),
            new Option("output",    OptionKind.String,  "Input & Output", "Output directory (default: current directory).",                                     apply: (o, v) => o.OutputDir = v,                                                     shortName: 'o'),
            new Option("session",   OptionKind.String,  "Input & Output", "Resume session file path (default: <output>/resume.session).",                    apply: (o, v) => o.SessionFile = v,                                                shortName: 's'),
            new Option("keep-list", OptionKind.Flag,    "Input & Output", "Keep list files after the run instead of prompting.",                                apply: (o, _) => o.KeepListFiles = true,                                             shortName: 'k'),
            new Option("resume",    OptionKind.Flag,    "Input & Output", "Resume from the session file without prompting.",                                  apply: (o, _) => o.Resume = true),
            new Option("yes",       OptionKind.Flag,    "Input & Output", "Assume yes to every prompt (non-interactive).",                                 apply: (o, _) => o.Yes = true),

            // ── TRANSFER
            new Option("jobs",           OptionKind.Integer, "Transfer", "Maximum concurrent downloads.",                     min: 1, max: 16, valueName: "<n>", defaultValue: "1", apply: (o, v) => o.MaxConcurrentDownloads = ParseInt(v, "Jobs", 1, 16),    shortName: 'j'),
            new Option("connections",    OptionKind.Integer, "Transfer", "Per-server connections; also sets split and per-server.", min: 1, max: 16, valueName: "<n>", defaultValue: "16", apply: (o, v) => { var n = ParseInt(v, "Connections", 1, 16); o.Connections = n; o.Split = n; o.MaxConnectionPerServer = n; }, shortName: 'c'),
            new Option("split",          OptionKind.Integer, "Transfer", "Connections per download.",                 min: 1, max: 16, valueName: "<n>", defaultValue: "16", apply: (o, v) => o.Split = ParseInt(v, "Split", 1, 16)),
            new Option("per-server",     OptionKind.Integer, "Transfer", "Maximum connections per server.",            min: 1, max: 16, valueName: "<n>", defaultValue: "16", apply: (o, v) => o.MaxConnectionPerServer = ParseInt(v, "Per-server", 1, 16)),
            new Option("min-split-size", OptionKind.String,  "Transfer", "Minimum piece size, e.g. 20M.",              valueName: "<size>", apply: (o, v) => o.ExtraOptions = o.ExtraOptions.Append($"min-split-size={v}").ToArray()),
            new Option("limit",          OptionKind.String,  "Transfer", "Max overall download speed, e.g. 500K or 2M.", valueName: "<limit>", apply: (o, v) => o.SpeedLimit = NormalizeSpeedLimit(v), shortName: 'l'),
            new Option("aria2-opt",      OptionKind.List,    "Transfer", "Pass an aria2 option key=value (repeatable).", valueName: "<key=value>", apply: (o, v) => o.ExtraOptions = o.ExtraOptions.Append(v ?? string.Empty).ToArray()),
            new Option("force",          OptionKind.Flag,    "Transfer", "Overwrite existing files.",                                apply: (o, _) => o.Force = true,                                           shortName: 'f'),
            new Option("auto-file-renaming", OptionKind.Flag, "Transfer", "Let aria2 rename colliding files.",                        apply: (o, _) => o.AutoFileRenaming = true),

            // ── DISPLAY
            new Option("inline",      OptionKind.Flag,    "Display", "Inline dashboard instead of full-screen.",   apply: (o, _) => o.Inline = true),
            new Option("plain",       OptionKind.Flag,    "Display", "Plain text output, no escape codes.",          apply: (o, _) => o.Plain = true),
            new Option("no-pager",    OptionKind.Flag,    "Display", "Disable the help pager.",                       apply: (o, _) => o.NoPager = true),
            new Option("no-color",    OptionKind.Flag,    "Display", "Disable colour output.",                        apply: (o, _) => o.NoColor = true),
            new Option("ascii",       OptionKind.Flag,    "Display", "Use ASCII-only glyphs.",                         apply: (o, _) => o.Ascii = true),
            new Option("refresh",     OptionKind.Integer, "Display", "Dashboard refresh interval in ms.", min: 100, max: 5000, valueName: "<ms>", defaultValue: "500", apply: (o, v) => o.RefreshIntervalMs = ParseInt(v, "Refresh", 100, 5000)),
            new Option("poll",        OptionKind.Integer, "Display", "Engine poll interval in ms (default: refresh).", min: 100, max: 60000, valueName: "<ms>", apply: (o, v) => o.PollIntervalMs = ParseInt(v, "Poll", 100, 60000)),
            new Option("verbose",     OptionKind.Flag,    "Display", "Verbose logging.",                            apply: (o, _) => o.Verbose = true,                                           shortName: 'v'),

            // ── SETUP
            new Option("install-deps", OptionKind.Flag, "Setup", "Install aria2 into a user-local tools dir (GitHub release; no admin).", apply: (o, _) => o.InstallDeps = true, shortName: 'y'),
            new Option("no-install",  OptionKind.Flag, "Setup", "Never install anything (wins over --install-deps).", apply: (o, _) => o.NoInstall = true, shortName: 'n'),
            new Option("install-aria2", OptionKind.Flag, "Setup", "Install aria2 if it is missing.", apply: (o, _) => o.InstallAria2 = true),
            new Option("aria2-path",  OptionKind.String, "Setup", "Path to the aria2c binary.", valueName: "<path>", apply: (o, v) => o.Aria2Path = v),

            // ── CONFIG
            new Option("config",        OptionKind.String, "Config", "Path to a settings file.", valueName: "<path>", apply: (o, v) => o.ConfigFile = v),
            new Option("print-config",  OptionKind.Flag,   "Config", "Print the resolved settings and exit.", apply: (o, _) => o.PrintConfig = true),

            // ── META
            new Option("help",    OptionKind.Flag, "Meta", "Show this help and exit.",   apply: (o, _) => o.Help = true, shortName: 'h'),
            new Option("version", OptionKind.Flag, "Meta", "Print the version and exit.", apply: (o, _) => o.Version = true, shortName: 'V'),
            new Option("doctor",  OptionKind.Flag, "Meta", "Diagnose network, package managers, and aria2.", apply: (o, _) => o.Doctor = true),
        };
    }

    private static int ParseInt(string? value, string title, int min, int max)
    {
        if (value is null || !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) || n < min || n > max)
            throw new CliException($"{title} must be a number from {min} to {max}. Run --help for usage.");
        return n;
    }

    /// <summary>
    /// Normalise a speed limit as dneat does: 500K / 2M keep their suffix (upper-cased); fractional
    /// values such as 1.5M are converted to an integer K (1.5M -> 1536K).
    /// </summary>
    private static string NormalizeSpeedLimit(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new CliException("Missing value for option --limit. Run --help for usage.");

        var s = value.Trim();
        var unit = '\0';
        if (s.Length > 0 && "KkMm".AsSpan().Contains(s[^1]))
            unit = char.ToUpperInvariant(s[^1]);
        var number = unit == '\0' ? s : s.Substring(0, s.Length - 1);
        if (!decimal.TryParse(number, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) || v < 0)
            throw new CliException($"Invalid speed limit '{value}'. Use a number with an optional K or M, e.g. 500K or 2M. Run --help for usage.");

        if (s.AsSpan().IndexOfAny(['.', ',']) >= 0)
        {
            var k = unit switch { 'M' => v * 1024, 'K' => v, _ => v / 1024 };
            k = k < 1 ? 1 : k;
            return $"{(long)k}K";
        }

        return unit == '\0' ? $"{v:0}" : $"{v}{(unit == 'M' ? "M" : "K")}";
    }
}