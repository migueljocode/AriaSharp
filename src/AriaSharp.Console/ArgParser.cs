using System.Text;
using AriaSharp.Models;

namespace AriaSharp.Console;

public static class ArgParser
{
    public static AppSettings Parse(string[] args)
    {
        var settings = new AppSettings();
        var inputs = new List<string>();
        var extra = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "-h" or "--help": settings.Help = true; break;
                case "-V" or "--version": settings.Version = true; break;
                case "-i" or "--input": inputs.AddRange(TakeValue(args, ref i)); break;
                case "-f" or "--file": inputs.AddRange(TakeValue(args, ref i)); break;
                case "-d" or "--dir": settings.OutputDir = TakeValueFirst(args, ref i); break;
                case "-s" or "--session": settings.SessionFile = TakeValueFirst(args, ref i); break;
                case "-c" or "--config": settings.ConfigFile = TakeValueFirst(args, ref i); break;
                case "-a" or "--aria2-path": settings.Aria2Path = TakeValueFirst(args, ref i); break;
                case "-o" or "--aria2-opt": settings.Aria2Opt = TakeValueFirst(args, ref i); break;
                case "-l" or "--speed-limit": settings.SpeedLimit = TakeValueFirst(args, ref i); break;
                case "-n" or "--no-color": settings.NoColor = true; break;
                case "-p" or "--plain": settings.Plain = true; break;
                case "-e" or "--inline": settings.Inline = true; break;
                case "-P" or "--no-pager": settings.NoPager = true; break;
                case "-v" or "--verbose": settings.Verbose = true; break;
                case "-y" or "--yes": settings.Yes = true; break;
                case "-F" or "--force": settings.Force = true; break;
                case "-r" or "--resume": settings.Resume = true; break;
                case "-k" or "--keep-list-files": settings.KeepListFiles = true; break;
                case "--probe-sizes": settings.ProbeSizes = true; break;
                case "--install-deps": settings.InstallDeps = true; break;
                case "--install-aria2": settings.InstallAria2 = true; break;
                default:
                    if (arg.StartsWith("--", StringComparison.Ordinal)) extra.Add(arg);
                    else inputs.Add(arg);
                    break;
            }
        }
        if (inputs.Count > 0) settings.Inputs = inputs.ToArray();
        settings.Inputs = inputs.Count > 0 ? inputs.ToArray() : settings.Inputs;
        if (extra.Count > 0) settings.ExtraOptions = extra.ToArray();
        return settings;
    }

    private static string[] TakeValue(string[] args, ref int i)
    {
        if (i + 1 < args.Length) { var v = args[++i]; return new[] { v }; }
        return Array.Empty<string>();
    }

    private static string TakeValueFirst(string[] args, ref int i)
    {
        var vals = TakeValue(args, ref i);
        return vals.Length > 0 ? vals[0] : string.Empty;
    }
}