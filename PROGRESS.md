# PROGRESS.md

## Milestone
CLI option correctness + M2–M4 on feat/rebuild.

## Done
- **`-y` → `--yes`** (was wrongly `--install-deps`)
- `--install-deps` long-only; `-n` still `--no-install`
- `--yes` sets KeepListFiles + Resume; enables aria2 install attempt if missing
- ArgParser coverage for shorts + core long flags + value forms
- **Verified:** 141 tests; print-config shows Yes/Force/AllowOverwrite

## Next
- Watch CI on PR #1
- Optional installers polish

## Blocked
- (none)
