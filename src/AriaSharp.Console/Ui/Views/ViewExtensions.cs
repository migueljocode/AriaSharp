namespace AriaSharp.App;

/// <summary>C# 14 extension members for UI views.</summary>
internal static class ViewExtensions
{
    extension(SessionState state)
    {
        public IRenderable ToDashboard(TimeSpan elapsed, int split = 16, int frame = -1, double? shown = null) =>
            DashboardView.Build(state, elapsed, split, frame, shown, split);

        public void WriteInlineProgress(TextWriter writer, TimeSpan elapsed, bool color) =>
            InlineView.WriteProgress(writer, state, elapsed, color);

        public void WritePlainProgress(TextWriter writer, TimeSpan elapsed, bool interactive) =>
            PlainView.WriteProgress(writer, state, elapsed, interactive);

        public IRenderable ToSummary(
            RunOutcome outcome,
            TimeSpan elapsed,
            string outputDir,
            string sessionPath,
            bool showRetryPrompt = false) =>
            SummaryView.Build(state, outcome, elapsed, outputDir, sessionPath, showRetryPrompt);

        public void WriteSummary(
            RunOutcome outcome,
            TimeSpan elapsed,
            string outputDir,
            string sessionPath,
            bool color,
            bool showRetryPrompt = false) =>
            SummaryView.Write(state, outcome, elapsed, outputDir, sessionPath, color, showRetryPrompt);
    }

    extension(AppSettings settings)
    {
        public void ShowHelp() => HelpView.Render(settings);

        public string HelpDocumentText() => HelpDocument.Render(settings);
    }
}
