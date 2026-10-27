namespace AriaSharp.App;

/// <summary>
/// A small, dependency-free argument parser driven entirely by <see cref="OptionTable"/>. It
/// understands <c>--opt value</c>, <c>--opt=value</c>, short clusters (<c>-fkv</c>) and a
/// <c>--</c> terminator. Result is a <see cref="SettingsOverlay"/> where unset options are null,
/// so the composition root can layer it over config-file and environment defaults.
/// </summary>
internal static class ArgParser
{
    extension(string[] args)
    {
        public SettingsOverlay ParseCli()
        {
        var overlay = new SettingsOverlay();
        var terminate = false;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (terminate)
            {
                overlay.Inputs = overlay.Inputs.Append(arg).ToArray();
                continue;
            }

            if (arg == "--")
            {
                terminate = true;
                continue;
            }

            if (arg.StartsWith("--", StringComparison.Ordinal))
            {
                i = HandleLong(arg.AsSpan(2), overlay, i, args);
                continue;
            }

            if (arg.Length > 1 && arg[0] == '-')
            {
                i = HandleShort(arg.AsSpan(1), overlay, i, args);
                continue;
            }

            overlay.Inputs = overlay.Inputs.Append(arg).ToArray();
        }

        return overlay;
        }
    }

    public static SettingsOverlay Parse(string[] args)
    {
        ThrowIfNull(args);
        return args.ParseCli();
    }

    private static int HandleLong(ReadOnlySpan<char> body, SettingsOverlay overlay, int i, string[] argv)
    {
        var eq = body.IndexOf('=');
        var name = (eq < 0 ? body : body[..eq]).ToString();
        var inlineValue = eq < 0 ? null : body[(eq + 1)..].ToString();

        var opt = OptionTable.FindByLong(name);
        if (opt is null)
            throw new CliException($"Unknown option: --{name}. Run --help for usage.");

        if (!opt.TakesValue)
        {
            if (inlineValue is not null)
                throw new CliException($"Option --{name} does not take a value. Run --help for usage.");
            opt.Apply!(overlay, null);
            return i;
        }

        var value = inlineValue ?? TakeNextValue(i, argv, name);
        opt.Apply!(overlay, value);
        return inlineValue is null ? i + 1 : i;
    }

    private static int HandleShort(ReadOnlySpan<char> cluster, SettingsOverlay overlay, int i, string[] argv)
    {
        var ch = cluster[0];
        var opt = OptionTable.FindByShort(ch);
        if (opt is null)
            throw new CliException($"Unknown option: -{ch}. Run --help for usage.");

        if (!opt.TakesValue || cluster.Length == 1)
        {
            if (!opt.TakesValue)
            {
                opt.Apply!(overlay, null);
                // remaining chars in the cluster are more flags, e.g. -fkv
                for (var c = 1; c < cluster.Length; c++)
                {
                    var next = OptionTable.FindByShort(cluster[c]);
                    if (next is null || next.TakesValue)
                        throw new CliException($"Unknown option: -{cluster[c]}. Run --help for usage.");
                    next.Apply!(overlay, null);
                }
                return i;
            }

            // value flag without attached value: -j 8  (value is the next argument)
            var value = TakeNextValue(i, argv, opt.LongName);
            opt.Apply!(overlay, value);
            return i + 1;
        }

        // value option with an attached value: -j8
        opt.Apply!(overlay, cluster[1..].ToString());
        return i;
    }

    private static string TakeNextValue(int i, string[] argv, string optionName)
    {
        if (i + 1 < argv.Length) return argv[i + 1];
        throw new CliException($"Missing value for option --{optionName}. Run --help for usage.");
    }
}
