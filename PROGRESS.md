# PROGRESS.md

## Milestone
M2 complete + post-run summary + failed-exit mapping.

## Done
- Program → real Aria2Engine → DownloadSession end-to-end
- DownloadSession classifies Completed / Failed / Empty / Interrupted
- SummaryView: Result, Size, Speed, Time, Output, Resume (+ failed/complete lists)
- ErrorCode on DownloadItem; mapped from aria2 tellStatus
- DownloadSessionTests (scripted engine): complete / failed / empty
- **Verified:** 112 tests green; example.com exit 0; 404 exit 1 with "Resource not found"

## Next
- Live progress line in --plain mode while running
- WebSocket notifications
- Full-screen dashboard (M4)

## Blocked
- (none)
