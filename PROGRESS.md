# PROGRESS.md

## Milestone
PR #2 (installer QoS) merged to main.

## Done
- Package-manager probe + pinned GitHub fallback (SHA-256, TLS source build)
- TTY vs consent: `nonInteractive` required; `--install-deps` and download path both pass it
- Release-host HEAD gates artifact fallback only
- --doctor exit 1 without aria2/HTTPS; CI smoke asserts it
- ScreenGuard.EmergencyRestore on second Ctrl-C

## Next
- Inject process runner / network probe for deterministic consent + fallback unit tests
  (manager declined, fallback declined, release host unreachable)

## Blocked
- (none)
