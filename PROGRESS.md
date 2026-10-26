# PROGRESS.md

## Milestone
M2 prep: fix remaining Brief defects in input + progress (magnet, sanitize, complete-item totals). Verified locally.

## Done (S0 / S2)
- S2 OptionTable-driven args, layered settings (98 tests at that point)
- Prior main stabilize work

## Done (this session)
- UrlExtractor: magnet:? links matched
- UrlSanitizer.Sanitize: keeps URL-valid chars; only controls + Spectre [] 
- ProgressCalculator: complete/failed/skipped/interrupted included in totals
- Aria2Locator: OperatingSystem.IsWindows + Path.Join
- New UrlTests + ProgressCalculatorTests; analyzer clean
- **Local verification (sandbox):**
  - .NET SDK 10.0.403 installed
  - aria2c 1.37.0 installed
  - `dotnet build -c Release`: 0 errors, 0 warnings
  - `dotnet test -c Release`: **109 passed**, 0 failed

## Next
- M2: real Aria2Engine (process + owner-only conf with rpc-secret, JSON-RPC multicall snapshot, saveSession, shutdown) + wire Program → DownloadSession
- Keep honest exit-1 until engine is live

## Blocked
- (none for unit work)

## Decisions
- Console types internal by default
- Console namespace: AriaSharp.App
- NoWarn: CA1724, CA1822 only
- graphify: AST-only, graphify-out/ not committed
- Sanitize keeps URL-valid characters
