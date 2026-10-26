namespace AriaSharp.App;

internal static class Logo
{
public static readonly string[] Ascii = new[]
{
    "AAAA   RRRR   IIIIII  SSSSS  HH  HH  AAAA   RRRR   PPPPP ",
    "A  A   R   R     II   S      HH  HH  A  A   R   R   P   P ",
    "AAAAA  RRRR     II    SSSSS  HHHHH  AAAAA  RRRR    PPPPP ",
    "A  A   R R     II        S   HH  HH  A  A   R R     P    ",
    "A  A   R  R  IIIIII  SSSSS   HH  HH  A  A   R  R   PPPP  ",
    "                                                    ",
    "   aria2 client for .NET                          ",
};

    public static void Render(int width, bool stacked = false)
    {
        foreach (var line in Ascii) System.Console.WriteLine(line);
    }
}