# PROGRESS.md

## Milestone
M4: dashboard + cancel keys + alternate screen.

## Done
- Purple Live dashboard (default interactive ANSI)
- **ScreenGuard**: alternate buffer + hide cursor; restore on Dispose
- **KeyListener**: `q` / `Esc` → cancel session (exit 130)
- Ctrl+C still handled via CancelKeyPress
- Plain/--no-color fallback unchanged
- **Verified:** 126 tests; plain smoke exit 0

## Next
- Width snapshot tests for DashboardView
- Inline mode polish
- Installer scripts (compile aria2) if desired

## Blocked
- (none)
