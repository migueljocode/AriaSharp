namespace AriaSharp.Models;

/// <summary>C# 14 extension members for aria2 error codes.</summary>
public static class ErrorExtensions
{
    extension(int code)
    {
        public string Aria2Text() => ErrorCatalog.TextFor(code);

        public string Aria2Hint() => ErrorCatalog.HintFor(code);

        public bool IsAria2Skipped() => ErrorCatalog.IsSkippedCode(code);
    }
}
