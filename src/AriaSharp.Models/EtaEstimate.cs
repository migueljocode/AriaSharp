namespace AriaSharp.Models;

public sealed class EtaEstimate
{
    private EtaEstimate(double? seconds, Confidence confidence) { Seconds = seconds; Confidence = confidence; }
    public double? Seconds { get; }
    public Confidence Confidence { get; }
    public static EtaEstimate Exact(double s) => new(s, Confidence.Exact);
    public static EtaEstimate AtLeast(double s) => new(s, Confidence.AtLeast);
    public static EtaEstimate Estimated(double s) => new(s, Confidence.Estimated);
    public static EtaEstimate Unknown => new(null, Confidence.Unknown);
    public string Format()
    {
        if (Seconds is not double s) return "--";
        var prefix = Confidence switch { Confidence.AtLeast => ">= ", Confidence.Estimated => "~ ", _ => "" };
        var ts = TimeSpan.FromSeconds(s);
        var dur = ts.TotalHours >= 1 ? $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}" : $"{(int)ts.TotalMinutes}:{ts.Seconds:D2}";
        return prefix + dur;
    }
}