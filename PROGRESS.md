# PROGRESS.md

## Milestone
M2 in progress: real Aria2Engine + purple Spectre help.

## Done
- S2 OptionTable / layered settings
- Input/progress fixes (magnet, sanitize, complete totals)
- **Aria2Engine (real):** process start, owner-only conf + rpc-secret, free loopback port, JSON-RPC getVersion + system.multicall snapshot, saveSession, shutdown
- **Aria2RpcClient:** thin AOT-safe HttpClient client (no Aria2.JsonRpcClient package)
- **HelpView:** Figlet + purple panels (falls back to plain with --no-color/--plain/--ascii)
- Local: .NET 10.0.403, aria2c 1.37.0, build 0/0, **109 tests green**

## Next
- Wire Program → RunWorkspace → Aria2Locator → Aria2ArgsBuilder → Aria2Engine → DownloadSession
- Integration smoke: start real aria2, getVersion, empty snapshot, shutdown
- WebSocket notifications (later)
- Full dashboard loop (M4)

## Blocked
- (none)

## Decisions
- Thin JSON-RPC client instead of Aria2.JsonRpcClient (AOT)
- Help is Spectre TUI by default; plain text opt-out via --no-color/--plain/--ascii
