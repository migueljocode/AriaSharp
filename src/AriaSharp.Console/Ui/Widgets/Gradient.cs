
namespace AriaSharp.App;

internal readonly record struct Rgb(byte R, byte G, byte B)
{
    public string Hex => $"#{R:X2}{G:X2}{B:X2}";

    public static Rgb Lerp(Rgb a, Rgb b, double t) => new(
        (byte)Math.Round(a.R + ((b.R - a.R) * t)),
        (byte)Math.Round(a.G + ((b.G - a.G) * t)),
        (byte)Math.Round(a.B + ((b.B - a.B) * t)));
}

/// <summary>Pure gradient, sub-cell progress bar and motion helpers. Emits Spectre markup; no Spectre types.</summary>
internal static class Gradient
{
    public static readonly Rgb[] Violet = [new(0x5B, 0x21, 0xB6), new(0x7C, 0x3A, 0xED), new(0x8B, 0x5C, 0xF6), new(0xA7, 0x8B, 0xFA), new(0xDD, 0xD6, 0xFE)];
    public static readonly Rgb[] Green = [new(0x06, 0x5F, 0x46), new(0x05, 0x96, 0x69), new(0x10, 0xB9, 0x81), new(0x34, 0xD3, 0x99), new(0xA7, 0xF3, 0xD0)];
    public static readonly Rgb[] Yellow = [new(0x85, 0x4D, 0x0E), new(0xCA, 0x8A, 0x04), new(0xEA, 0xB3, 0x08), new(0xFA, 0xCC, 0x15), new(0xFE, 0xF0, 0x8A)];

    private static readonly Rgb Track = new(0x2D, 0x25, 0x40);
    private const string Eighths = " ▏▎▍▌▋▊▉";
    private const string SpinnerFrames = "⠋⠙⠹⠸⠼⠴⠦⠧⠇⠏";

    public static Rgb Sample(ReadOnlySpan<Rgb> stops, double t)
    {
        if (stops.Length == 1) return stops[0];
        var x = Math.Clamp(double.IsNaN(t) ? 0 : t, 0, 1) * (stops.Length - 1);
        var i = Math.Min((int)x, stops.Length - 2);
        return Rgb.Lerp(stops[i], stops[i + 1], x - i);
    }

    public static char Spinner(int frame) => SpinnerFrames[Math.Abs(frame / 2) % SpinnerFrames.Length];

    /// <summary>Exponential approach of <paramref name="shown"/> to <paramref name="target"/> (frame-rate independent glide).</summary>
    public static double Glide(double shown, double target, double dtSeconds, double rate = 7) =>
        shown + ((target - shown) * (1 - Math.Exp(-Math.Max(0, dtSeconds) * rate)));

    /// <summary>Soft logo-style colour for a position <paramref name="x"/> (0..1) at a given animation phase.</summary>
    public static Rgb Soft(ReadOnlySpan<Rgb> stops, double x, double phase)
    {
        var u = x + phase;
        var t = 1 - Math.Abs((2 * (u - Math.Floor(u))) - 1);
        return Sample(stops, 0.25 + (t * 0.75));
    }

    /// <summary>
    /// Bar exactly <paramref name="width"/> cells wide. Filled part uses the same soft logo gradient;
    /// frame drives a slow phase drift, a negative frame gives a static bar.
    /// </summary>
    public static string Bar(int width, double fraction, ReadOnlySpan<Rgb> stops, int frame = -1)
    {
        if (width <= 0) return string.Empty;
        var f = Math.Clamp(double.IsNaN(fraction) ? 0 : fraction, 0, 1);
        var eighths = (int)Math.Round(f * width * 8);
        var full = eighths / 8;
        var part = eighths % 8;
        var phase = frame < 0 ? 0 : frame * 0.012;

        var sb = new StringBuilder(width * 12);
        for (var x = 0; x < full; x++)
        {
            var c = Soft(stops, width <= 1 ? 0 : (double)x / (width - 1), phase);
            sb.Append('[').Append(c.Hex).Append(']').Append('█').Append("[/]");
        }

        var cells = full;
        if (part > 0 && cells < width)
        {
            var c = Soft(stops, width <= 1 ? 0 : (double)full / (width - 1), phase);
            sb.Append('[').Append(c.Hex).Append(" on ").Append(Track.Hex).Append(']').Append(Eighths[part]).Append("[/]");
            cells++;
        }

        if (cells < width)
            sb.Append('[').Append(Track.Hex).Append(']').Append(new string('░', width - cells)).Append("[/]");
        return sb.ToString();
    }

    /// <summary>Per-character gradient text with a slow ping-pong phase; characters right of <paramref name="reveal"/> (0..1) are blanked.</summary>
    public static string Text(string line, ReadOnlySpan<Rgb> stops, int frame = 0, double reveal = 1)
    {
        var sb = new StringBuilder(line.Length * 12);
        var phase = frame * 0.012;
        for (var x = 0; x < line.Length; x++)
        {
            var ch = line[x];
            var visible = line.Length == 0 || (double)x / line.Length <= reveal;
            if (!visible || ch == ' ')
            {
                sb.Append(visible ? ch : ' ');
                continue;
            }

            sb.Append('[').Append(Soft(stops, (double)x / line.Length, phase).Hex).Append(']').Append(ch).Append("[/]");
        }

        return sb.ToString();
    }
}
