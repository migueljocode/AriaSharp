# PROGRESS.md

## Milestone
Cross-platform deps + UI polish on feat/rebuild.

## Done
- Smart inputs, clipboard, dashboard, pager, retry, pause count, skip-existing
- OS package managers: apt/dnf/yum/pacman/zypper/apk · brew/port · winget/choco/scoop
- Clipboard helpers only on Linux; macOS/Windows use system clipboard
- Logo top margin on dashboard + help
- Gradient bars, render loop, session QoS (other session)
- **Verified:** tests green locally

## Next
- CI green on PR #1
- Optional: SHA-256 pin for Windows zip fallback installer

## Blocked
- (none)
