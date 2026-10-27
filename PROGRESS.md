# PROGRESS.md

## Milestone
User-local aria2 bootstrap (no sudo) + review fixes.

## Done
- PrerequisiteInstaller downloads official aria2 1.37.0 from GitHub
- Windows: zip → extract aria2c.exe → `AppPaths.ToolsBinDirectory`
- Linux/macOS: source tarball → configure/make/install → copy aria2c
- Cache cleaned after install; locator prefers bundled path
- Windows RPC conf: icacls restrict to current user
- Clipboard: probe only; no privileged package install
- Logo top margin; OS-aware paths under XDG / LOCALAPPDATA

## Next
- CI green on PR #1
- Optional Program.cs split into InstallCommand / DownloadCommand

## Blocked
- (none)
