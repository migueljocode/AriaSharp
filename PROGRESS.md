# PROGRESS.md

## Milestone
Installer seams + consent/fallback unit tests (PR).

## Done
- IProcessRunner, IInstallEnvironment, INetworkProbe, IArtifactInstaller seams
- PrerequisiteInstaller instance flow + CreateDefault production wiring
- Consent/fallback/sudo/classify tests with hand-written fakes (no network/process)
- End-of-run saveSession failure: one verbose warning line, no throw

## Next
- Optional S1 spike: Aria2.JsonRpcClient under PublishAot
- Terminal check: full-screen help and dashboard on a real TTY

## Blocked
- (none)
