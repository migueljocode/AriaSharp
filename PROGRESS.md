# PROGRESS.md

## Milestone
M0 Stabilize (S) — reproduce and fix the red CI; clean csproj files, packages and NoWarn; remove duplicate types; fix namespaces; add LICENSE; help exits 0; correct ErrorCatalog with tests. Exit: CI green on three OSes with an honest warning policy.

## Done
- Initial solution committed and pushed to main
- GitHub Actions CI workflow added and pushed (ubuntu job)
- Build: 0 errors, 0 warnings; Tests: 76 passed, 0 failed
- Console namespace fixed: AriaSharp.Console → AriaSharp.App (matches RootNamespace, section 71)
- docs/decisions/namespace.md recorded
- Duplicate types removed: IAria2Locator (Engine.cs, Setup.cs), IPrerequisiteInstaller (Setup.cs), DownloadSnapshot (Downloads.cs); duplicate IUrlNameGuesser removed from Input.cs
- ErrorCatalog corrected to aria2 1.37.0 codes 1-32 (section 4); Classify remapped
- ErrorCatalogTests.cs added; old UnitTests ErrorCatalog tests corrected
- Aria2ArgsBuilder rewritten: lowercase booleans, secret in conf file (not command line), --key=value opts with blocked-key validation, SaveSession honoured
- Aria2ArgsBuilderTests.cs added (8 tests)
- Terminal.Truncate fixed (no throw when max < 3); delegates to FormatHelpers.Truncate; test added
- Logo: block-letter ARIASHARP
- LICENSE (MIT) added
- ArgParser rewritten: dneat letters (-i -o -f -k -j -c -l -s -y -n -h -V), --opt=value, -- terminator, validation, short clusters; CliException added; help exits 0
- ArgParserTests.cs + Program help/exit tests added
- Console cleaned: OptionTable removed (dead/CA1812), helper types internal (CA1515), Program disposes Aria2Engine/HttpClient, async writes, ConfigureAwait
- csproj files cleaned: Models/Services/Console all net10.0, nullable, implicit usings, IsAotCompatible, Console PublishAot=true, single AssemblyName each
- NoWarn shrunk to minimum: CA1724;CA1031;CA1062;CA1515;CA2007 — each with a justification comment
- IL2026/IL3050 never suppressed per section 3 rule 4
- JsonSerializer source generation (AppSettingsJsonContext) added to fix IL3050
- Build green with empty-benign NoWarn; 76 tests pass

## Next
- Commit and push M0 stabilization
- Verify CI green on all three OSes (cannot run macOS/windows CI jobs locally)

## Blocked
- Cannot run macOS/windows CI jobs locally

## Decisions
- Namespace: AriaSharp.App (docs/decisions/namespace.md)
- CLI letters: -n = no-install (section 11); --no-color long-only due to conflict (record in docs/decisions/)
- Section 17 decisions 1, 3, 4, 5 open for owner
