# Solution format decision (.slnx)

## Decision
Migrate from the legacy `.sln` (MSBuild solution) to the SDK-style `.slnx` (XML solution) file.

## Rationale
- `.slnx` is the modern, XML-based solution format introduced with .NET 10 / SDK 10.
- It is human-readable, diff-friendly, and version-control friendly compared to the GUID-heavy `.sln` format.
- `dotnet sln migrate` converts the legacy file; all 4 projects (Models, Services, Console, Tests) are included under `/src` and `/tests`.
- All projects set `<Build Project="true" />` so `dotnet build AriaSharp.slnx` builds everything; tests are run via `dotnet test AriaSharp.slnx`.

## Files changed
- Deleted `AriaSharp.sln` (legacy, placeholder GUIDs)
- Added `AriaSharp.slnx` (XML solution)
- Updated `.github/workflows/ci.yml` (restore/build target `AriaSharp.slnx`)
- Updated `README.md` (build/test commands)

## Verification
- `dotnet sln AriaSharp.slnx list` shows all 4 projects
- `dotnet build AriaSharp.slnx -c Release`: 0 errors, 0 warnings
- `dotnet test AriaSharp.slnx -c Release`: 77 passed, 0 failed

## Global.json
- Pins SDK `10.0.113` with `rollForward: latestMinor`; SDK 10.0.113 is installed locally.
