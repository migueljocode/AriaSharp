using System.Text;

namespace AriaSharp.Console;

public sealed class OptionTable
{
    public sealed class Option
    {
        public string Long { get; init; } = string.Empty;
        public string Short { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string? DefaultValue { get; init; }
        public bool IsFlag { get; init; }
        public string Category { get; init; } = "General";
    }

    private readonly List<Option> _options = new();

    public void Add(Option option) { _options.Add(option); }
    public void AddRange(IEnumerable<Option> options) { _options.AddRange(options); }
    public IReadOnlyList<Option> All => _options;

    public void Render(StringBuilder sb, int width)
    {
        var groups = _options.GroupBy(o => o.Category);
        foreach (var group in groups)
        {
            sb.AppendLine(group.Key.ToUpperInvariant());
            foreach (var opt in group)
            {
                var flags = new List<string>();
                if (!string.IsNullOrEmpty(opt.Short)) flags.Add($"-{opt.Short}");
                if (!string.IsNullOrEmpty(opt.Long)) flags.Add($"--{opt.Long}");
                var flagStr = string.Join(", ", flags);
                var line = $"  {flagStr,-22} {opt.Description}";
                if (opt.DefaultValue != null) line += $"  [default: {opt.DefaultValue}]";
                sb.AppendLine(line);
            }
            sb.AppendLine();
        }
    }
}