# PROGRESS.md

## Milestone
Smart inputs + retry prompt.

## Done
- Resolve: CLI URLs · file paths (extract links) · stdin · clipboard
- `--from-clipboard` / `--from-file <path>` force modes
- On failure (interactive): prompt [R]etry / [Q]uit — not force-close
- **Verified:** 154 tests; list file → extracted example.com + example.org
- Review pass on feat/rebuild: paused count + P no longer ends the run; all-skipped run is Completed; Q/Ctrl-C during a snapshot settles and returns Interrupted; aria2 gets a 3 s grace to exit after shutdown before kill; credential masking is authority-only and shared (Aria2Process uses FormatHelpers); loopback RPC and WebSocket bypass proxies; aria2 output goes to the run log file; `AddUriAsync` returns the real GID (string)

## Next
- CI green on PR #1

## Blocked
- (none)
