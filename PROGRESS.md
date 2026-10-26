# PROGRESS.md

## Milestone
M2–M4 on feat/rebuild: engine, session, dashboard, inline, throttle.

## Done
- Real Aria2Engine (RPC, multicall, WS notifications, owner-only conf)
- DownloadSession outcomes (Completed/Failed/Empty/Interrupted) + SummaryView
- Live plain progress + ProgressThrottle (~1 Hz when redirected)
- Purple Spectre Live dashboard, ScreenGuard, q/Esc cancel
- --inline single-line mode
- Dashboard width snapshots
- **Verified:** 137 tests; example.com exit 0; 404 exit 1

## Next
- Open PR → main (16+ commits)
- Installer scripts optional

## Blocked
- (none)
