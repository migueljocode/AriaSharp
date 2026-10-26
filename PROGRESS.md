# PROGRESS.md

## Milestone
S1: Solution in src/, folders, packages, CI, AGENTS.md — restructure per REBUILD prompt section S1.

## Done (S0)
- git clone https://github.com/migueljocode/AriaSharp, branch feat/rebuild
- Read Brief, PROGRESS.md, git log
- graphifyy installed (0.9.90)
- Baseline: dotnet build 0 warnings/0 errors, dotnet test 89 passed/0 failed
- Cloned WorkWithIO/dneat to /tmp/dneat, extracted aria2 flags

## Done (S1 - in progress)
- Created src/AriaSharp.slnx with exact XML spec (4 projects under /src and /tests folder)
- Moved AriaSharp.slnx from root to src/
- Moved all source files into target folders with git mv (Models/Contracts, Downloads, Engine, Session, Settings, Errors, Formatting; Services/Engine, Engine/Rpc, Session, Input, IO, Config, Setup; Console/Cli, Terminal, Ui/Theme, Ui/Widgets, Ui/Views, Ui/Help)
- Split multi-type files: Contracts.cs → Contracts/Contracts.cs + Engine/EngineSettings.cs; Engine.cs → Aria2Locator.cs + Aria2ArgsBuilder.cs + Aria2Process.cs; LogWriter.cs separated
- Deleted replaced files: Pager.cs, ProtocolColors.cs, Widgets.cs, old SessionStore.cs, Engine.cs
- .editorconfig: added dotnet_diagnostic.IDE0130.severity = none
- Directory.Packages.props: pruned to Spectre.Console 0.57.2, Spectre.Console.Testing 0.57.2, xunit, xunit.runner.visualstudio, Microsoft.NET.Test.Sdk, coverlet.collector
- Added PackageReference Spectre.Console to Console, Spectre.Console.Testing to Tests
- Directory.Build.props: added Using for System.Globalization, System.Text, AriaSharp.Models; NoWarn reduced to CA1724;CA1822 with justifications
- ci.yml: rewritten with ubuntu/macos/windows matrix, aria2 install per OS, AOT publish per RID, smoke tests
- .graphifyignore and .gitignore already have graphify-out/
- Fixed all CA errors (CA1031, CA2007, CA1062, CA1515, CA1822) by fixing code, not suppressing
- Made all Console types internal per CA1515/AGENTS.md
- Added InternalsVisibleTo for AriaSharp.Tests in Console csproj
- Build: 0 warnings, 0 errors; Tests: 89 passed, 0 failed

## Next
- Complete S1 commit
- Verify CI green on 3 OSes
- Proceed to S2: Arguments (OptionTable, overlay parser, layered settings)

## Blocked
- (none)

## Decisions
- Console types internal by default (CA1515/AGENTS.md)
- Console namespace: AriaSharp.App (matches RootNamespace, not "Console")
- NoWarn: CA1724 (false positive in .NET 10), CA1822 (interface methods must be instance)
- graphify: AST-only, graphify-out/ not committed