# PROGRESS.md

## Milestone
M2: real Aria2Engine wired end-to-end.

## Done
- Program → RunWorkspace → Aria2Locator → Aria2ArgsBuilder → Aria2Engine → DownloadSession
- Live RPC (getVersion + system.multicall snapshot), saveSession, shutdown
- Owner-only conf with rpc-secret (never on CLI)
- Purple Spectre help (plain fallback)
- **Verified:** build 0/0, 109 tests green, `ariasharp -o /tmp/... https://example.com/` → exit 0 + index.html

## Next
- DownloadSession: treat failed items as Failed outcome; richer progress
- Plain/inline summary output after run
- WebSocket notifications
- Dashboard TUI loop (M4)

## Blocked
- (none)
