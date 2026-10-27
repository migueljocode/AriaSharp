# PROGRESS.md

## Milestone
PR #2 merge polish: fallback consent, sudo -n, tighter failure classify, release HEAD.

## Done
- Consent before package manager **and** artifact fallback; decline skips fallback with clear message
- `sudo -n` when non-interactive; TTY/password errors → Auth
- ClassifyManagerFailure: lock phrase only; no empty log lines
- NetworkProbe HEAD on release URL (5s) before generic TCP/DNS
- --doctor single HTTPS line; trim CR; exit 1 when missing/no HTTPS
- Real consent unit test (when aria2 absent)

## Next
- Merge PR #2 when CI green on tip

## Blocked
- (none)
