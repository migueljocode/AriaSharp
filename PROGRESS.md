# PROGRESS.md

## Milestone
S2: Arguments — single OptionTable driving parser/help/validation/config keys, overlay-based layered settings, full coverage.

## Done (S0)
- git clone https://github.com/migueljocode/AriaSharp, branch feat/rebuild
- Read Brief, PROGRESS.md, git log
- graphifyy installed (0.9.90)
- Baseline: dotnet build 0 warnings/0 errors, dotnet test 89 passed/0 failed
- Cloned WorkWithIO/dneat to /tmp/dneat, extracted aria2 flags

## Done (S2)
- OptionTable.cs: single source of truth for all CLI options (names, shorts, kinds, ranges, categories, help, Apply). Categories static exposed for grouping.
- ArgParser.cs: table-driven parser → SettingsOverlay; supports --opt value, --opt=value, short clusters (-fkv, -j8), -- terminator, validation with --help hint. Fixed double-advance on inline-value form.
- AppSettings.cs: AppSettings (resolved, non-nullable defaults) + SettingsOverlay (nullable layer) + AppDefaults + AppSettingsJsonContext. Inputs/ExtraOptions non-nullable.
- SettingsStore.cs: ISettingsStore/SettingsStore now LoadOverlayAsync/SaveOverlayAsync returning SettingsOverlay (overlay layer semantics); atomic write (tmp+File.Move); corrupt→empty overlay.
- SettingsMerger: Merge(baseline, layers) — later layers win, null = "not set here" → falls back to baseline (defaults). Enables reset-to-default by omitting a field.
- EnvOverlay.cs: NO_COLOR/ARIASHARP_* env vars mapped per OptionTable keys, including verbose, no-color, ascii, config, network, and generic ARIASHARP_<KEY> for all value/flag options.
- HelpDocument.cs (renamed from HelpContent): generated entirely from OptionTable (groups, defaults, exit codes, env precedence, pager keys).
- Program.cs: composition root wiring defaults←config←env←CLI; Help/Version/PrintConfig/PrintConfig paths; --no-install clears install bits; honest "engine not implemented yet" stub exits 1 until M2.
- ArgParserTests: updated for SettingsOverlay (MaxConcurrentDownloads, nullable bool asserts) + Program.Main integration tests (help exits 0, no-input usage error exits 2, inputs → exit 1 "engine not implemented yet").
- FileIoTests: updated for LoadOverlayAsync/SaveOverlayAsync returning SettingsOverlay (corrupt→null fields).
- LayeredSettingsTests.cs (new): CLI>config>defaults precedence, null-fallback, layer-wins, bool-reset-to-false, reset-to-default-by-omit, inputs accumulation, OptionTable short-name/category sets, HelpDocument renders from OptionTable.
- Build: 0 warnings, 0 errors (TreatWarningsAsErrors, PublishAot, CA strict). Tests: 98 passed, 0 failed.

## Next
- Proceed to M2: DownloadSession + Engine against real aria2c (stub exits 1 honestly until then).

## Blocked
- (none)

## Decisions
- Console types internal by default (CA1515/AGENTS.md)
- Console namespace: AriaSharp.App (matches RootNamespace, not "Console")
- NoWarn: CA1724 (false positive in .NET 10), CA1822 (interface methods must be instance)
- graphify: AST-only, graphify-out/ not committed