# PROGRESS.md

## Milestone
Installer path + CLI letter restore (dneat parity).

## Done
- **-y / --install-deps** (Brief §11 / dneat); `--yes` long-only
- Standalone `ariasharp -y` / `--install-deps` **before** inputs check
- ProcessRunner drains stdout/stderr (no pipe hang)
- Package-manager install (apt/dnf/pacman/brew/winget), not source make
- Aria2Locator: absolute paths checked directly (no Path.Join bug)
- **Verified:** 143 tests; `-y` and `--install-deps` exit 0

## Next
- Help pager (alternate screen + less keys)
- Split Program / Setup further
- CI green on PR #1

## Blocked
- (none)
