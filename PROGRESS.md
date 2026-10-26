# PROGRESS.md

## Milestone
WebSocket notifications + live plain progress + M2.

## Done
- Aria2Engine end-to-end + DownloadSession classification + SummaryView
- Live plain progress (ProgressTick → PlainView)
- **Aria2NotificationListener**: ClientWebSocket on ws://127.0.0.1:port/jsonrpc
  maps onDownloadStart/Pause/Stop/Complete/Error + onBtDownloadComplete → EngineEvent channel
  best-effort (HTTP poll remains source of truth if WS fails)
- NotificationTests (TryMap) — **122 tests green**
- Smoke: example.com exit 0

## Next
- Full-screen purple dashboard (M4) consuming Events + snapshots
- Optional: surface notification events in verbose/plain output

## Blocked
- (none)
