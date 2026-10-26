using AriaSharp.Models;

namespace AriaSharp.App;

internal static class ArgParser
{
    public static AppSettings Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        var settings = new AppSettings();
        var inputs = new List<string>();
        var terminateOptions = false;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (terminateOptions)
            {
                inputs.Add(arg);
                continue;
            }
            if (arg == "--")
            {
                terminateOptions = true;
                continue;
            }
            if (arg.StartsWith("--", StringComparison.Ordinal))
            {
                HandleLong(arg, settings, inputs, ref i, args);
                continue;
            }
            if (arg.StartsWith('-') && arg.Length > 1)
            {
                HandleShortCluster(arg.AsSpan(1), settings, inputs, ref i, args);
                continue;
            }
            inputs.Add(arg);
        }

        if (inputs.Count > 0) settings.Inputs = inputs.ToArray();
        return settings;
    }

    private static void HandleLong(string arg, AppSettings settings, List<string> inputs, ref int i, string[] argv)
    {
        var body = arg.AsSpan(2);
        var eq = body.IndexOf('=');
        var name = eq < 0 ? body.ToString() : body[..eq].ToString();
        var value = eq < 0 ? null : body[(eq + 1)..].ToString();

        switch (name)
        {
            case "input":
                inputs.AddRange(TakeValue(value, ref i, argv, name));
                break;
            case "output":
                settings.OutputDir = TakeValueString(value, ref i, argv, name);
                break;
            case "force": settings.Force = true; break;
            case "keep-list": settings.KeepListFiles = true; break;
            case "jobs": settings.Jobs = TakeInt(value, ref i, argv, name, 1, 16); break;
            case "connections": settings.Connections = TakeInt(value, ref i, argv, name, 1, 16); break;
            case "limit": settings.SpeedLimit = TakeValueString(value, ref i, argv, name); break;
            case "session": settings.SessionFile = TakeValueString(value, ref i, argv, name); break;
            case "install-deps": settings.InstallDeps = true; break;
            case "no-install": settings.NoInstall = true; break;
            case "help": settings.Help = true; break;
            case "version": settings.Version = true; break;
            case "split": settings.Split = TakeInt(value, ref i, argv, name, 1, 16); break;
            case "per-server": settings.MaxConnectionPerServer = TakeInt(value, ref i, argv, name, 1, 16); break;
            case "min-split-size": settings.ExtraOptions = AddOpt(settings.ExtraOptions, $"min-split-size={TakeValueString(value, ref i, argv, name)}"); break;
            case "aria2-opt": settings.ExtraOptions = AddOpt(settings.ExtraOptions, TakeValueString(value, ref i, argv, name)); break;
            case "aria2-path": settings.Aria2Path = TakeValueString(value, ref i, argv, name); break;
            case "refresh": settings.RefreshIntervalMs = TakeInt(value, ref i, argv, name, 100, 5000); break;
            case "poll": settings.ExtraOptions = AddOpt(settings.ExtraOptions, $"poll={TakeValueString(value, ref i, argv, name)}"); break;
            case "inline": settings.Inline = true; break;
            case "plain": settings.Plain = true; break;
            case "no-pager": settings.NoPager = true; break;
            case "no-color": settings.NoColor = true; break;
            case "ascii": settings.ExtraOptions = AddOpt(settings.ExtraOptions, "ascii"); break;
            case "resume": settings.Resume = true; break;
            case "yes": settings.Yes = true; break;
            case "config": settings.ConfigFile = TakeValueString(value, ref i, argv, name); break;
            case "print-config": settings.ExtraOptions = AddOpt(settings.ExtraOptions, "print-config"); break;
            case "verbose": settings.Verbose = true; break;
            default: throw new CliException($"Unknown option: --{name}. Run --help for usage.");
        }
    }

    private static void HandleShortCluster(ReadOnlySpan<char> cluster, AppSettings settings, List<string> inputs, ref int i, string[] argv)
    {
        for (var c = 0; c < cluster.Length; c++)
        {
            var ch = cluster[c];
            var rest = cluster[(c + 1)..];
            switch (ch)
            {
                case 'i':
                    inputs.AddRange(TakeValue(rest.IsEmpty ? null : rest.ToString(), ref i, argv, "-i"));
                    c = cluster.Length; // value consumed rest of cluster or next arg
                    break;
                case 'o':
                    settings.OutputDir = TakeValueString(rest.IsEmpty ? null : rest.ToString(), ref i, argv, "-o");
                    c = cluster.Length;
                    break;
                case 'f': settings.Force = true; break;
                case 'k': settings.KeepListFiles = true; break;
                case 'j':
                    settings.Jobs = TakeInt(rest.IsEmpty ? null : rest.ToString(), ref i, argv, "-j", 1, 16);
                    c = cluster.Length;
                    break;
                case 'c':
                    settings.Connections = TakeInt(rest.IsEmpty ? null : rest.ToString(), ref i, argv, "-c", 1, 16);
                    c = cluster.Length;
                    break;
                case 'l':
                    settings.SpeedLimit = TakeValueString(rest.IsEmpty ? null : rest.ToString(), ref i, argv, "-l");
                    c = cluster.Length;
                    break;
                case 's':
                    settings.SessionFile = TakeValueString(rest.IsEmpty ? null : rest.ToString(), ref i, argv, "-s");
                    c = cluster.Length;
                    break;
                case 'y': settings.InstallDeps = true; break;
                case 'n': settings.NoInstall = true; break;
                case 'h': settings.Help = true; break;
                case 'V': settings.Version = true; break;
                case 'v': settings.Verbose = true; break;
                default: throw new CliException($"Unknown option: -{ch}. Run --help for usage.");
            }
        }
    }

    private static string[] TakeValue(string? value, ref int i, string[] argv, string name)
    {
        if (value != null) return new[] { value };
        if (i + 1 < argv.Length) return new[] { argv[++i] };
        throw new CliException($"Missing value for option {name}. Run --help for usage.");
    }

    private static string TakeValueString(string? value, ref int i, string[] argv, string name)
    {
        return TakeValue(value, ref i, argv, name)[0];
    }

    private static int TakeInt(string? value, ref int i, string[] argv, string name, int min, int max)
    {
        var text = TakeValueString(value, ref i, argv, name);
        if (!int.TryParse(text, out var result) || result < min || result > max)
            throw new CliException($"{name} must be a number from {min} to {max}. Run --help for usage.");
        return result;
    }

    private static List<string> AddOpt(IReadOnlyList<string> current, string opt)
    {
        var list = new List<string>(current) { opt };
        return list;
    }
}
