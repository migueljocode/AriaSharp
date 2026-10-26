# PROGRESS.md

## Milestone
Live plain progress + M2 complete.

## Done
- End-to-end Aria2Engine + DownloadSession
- Failed/empty/completed classification + SummaryView
- **Live progress line** via ProgressTick → PlainView.WriteProgress (CR when interactive, newline when redirected)
- PlainViewTests + DownloadSessionTests
- **Verified:** 114 tests; example.com shows progress ticks then Summary exit 0

## Next
- WebSocket notifications (onDownloadComplete/Error)
- Full-screen purple dashboard (M4)
- Optional: throttle progress when redirected

## Blocked
- (none)
