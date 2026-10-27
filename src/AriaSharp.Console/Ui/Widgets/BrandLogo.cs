namespace AriaSharp.App;

/// <summary>Shared ARIASHARP gradient logo for dashboard and help.</summary>
internal static class BrandLogo
{
    public const int TopMargin = 1;

    private static readonly string[] Lines =
    [
        " █████╗ ██████╗ ██╗ █████╗ ███████╗██╗  ██╗ █████╗ ██████╗ ██████╗ ",
        "██╔══██╗██╔══██╗██║██╔══██╗██╔════╝██║  ██║██╔══██╗██╔══██╗██╔══██╗",
        "███████║██████╔╝██║███████║███████╗███████║███████║██████╔╝██████╔╝",
        "██╔══██║██╔══██╗██║██╔══██║╚════██║██╔══██║██╔══██║██╔══██╗██╔═══╝ ",
        "██║  ██║██║  ██║██║██║  ██║███████║██║  ██║██║  ██║██║  ██║██║     ",
        "╚═╝  ╚═╝╚═╝  ╚═╝╚═╝╚═╝  ╚═╝╚══════╝╚═╝  ╚═╝╚═╝  ╚═╝╚═╝  ╚═╝╚═╝     ",
    ];

    public static IRenderable Build(int frame = -1)
    {
        var reveal = frame < 0 ? 1.0 : Math.Min(1.0, (frame + 1) / 14.0);
        var art = new Markup(string.Join('\n', Lines.Select(l => Gradient.Text(l, Gradient.Violet, Math.Max(frame, 0), reveal))));
        return Align.Center(art);
    }

    /// <summary>Rows: top margin blank + logo (for Grid-based views).</summary>
    public static void AddTo(Grid root, int frame = -1)
    {
        for (var i = 0; i < TopMargin; i++)
            root.AddRow(new Text(""));
        root.AddRow(Build(frame));
        root.AddRow(new Text(""));
    }
}
