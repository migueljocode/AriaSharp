# PROGRESS.md

## Milestone
Installer fix-up (PR #2): privileged process, fallback, consent, doctor exit.

## Done
- IsPrivilegedProcess for root detection
- Needs-root managers skipped → pinned zip/source fallback
- mayInstall = !NoInstall; autoYes separate; interactive y/N
- Package-manager failures classified + logged; fallback announced
- --doctor exits 1 if no aria2c / no HTTPS; CI asserts exit + HTTPS: yes
- ScreenGuard.EmergencyRestore on second Ctrl-C

## Next
- Merge PR #2 when CI green on latest tip
- Optional: deeper install flow tests with injected process runner

## Blocked
- (none)
