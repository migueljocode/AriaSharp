# PROGRESS.md

## Milestone
Help pager (less-style).

## Done
- HelpPager: alternate screen, j/k, space/b, g/G, /search, n/N, q/Esc
- Status line with position %; wrap on width; ScreenGuard restore
- --no-pager / redirected / non-TTY → full dump
- HelpView routes through HelpDocument + pager
- **Verified:** 147 tests; --help --no-pager prints full text

## Next
- CI green on PR #1
- Optional: Spectre colour inside pager pages

## Blocked
- (none)
