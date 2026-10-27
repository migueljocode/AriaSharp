# PROGRESS.md

## Milestone
PR #2 merge polish: TTY≠consent, release-host gate, drop unused InstallAsync.

## Done
- `nonInteractive` from Console.IsInputRedirected (sudo -n only without TTY); `-y` keeps plain sudo when interactive
- Artifact fallback gated on `CanReachReleaseHostAsync`; package managers need only general online
- Removed `IPrerequisiteInstaller.InstallAsync` (unused consent bypass)
- Consent tests are pure (resolve/classify/format); no process-wide env race skip

## Next
- Merge PR #2 when CI green on tip

## Blocked
- (none)
