# PROGRESS.md

## Milestone
Review cleanup: engine/download SRP, shared UI helpers, services console-free.

## Done
- User-local aria2 bootstrap from GitHub (no sudo)
- C# 14 extensions + global usings; top-level Program
- Aria2ConfWriter + Aria2SnapshotMapper extracted from Aria2Engine
- Aria2Bootstrap + DownloadSummary + DownloadPaths
- Aria2GitHubInstaller split from PrerequisiteInstaller
- InputResolver takes TextReader (no Console in Services)
- Shared FormatElapsed / TerminalMetrics
- Dashboard/Help/Summary section helpers

## Next
- CI green on PR #1; merge feat/rebuild → main
- Optional: more DownloadCommand plain-path tests with fakes

## Blocked
- (none)
