# PROGRESS.md

## Milestone
Installer QoS: package-manager probe, SHA-256 pins, doctor, logo widget.

## Done
- main @ 5f5384b merged; feat/rebuild removed
- PackageManagerCatalog + NetworkProbe
- Aria2 GitHub install: SHA-256 pin, TLS-only source build, install.log
- RPC HTTP error body preserved; conf deleted after RPC ready
- Second Ctrl-C hard-exits; --doctor diagnostics
- Shared BrandLogo; Models IDownloadSession only (no SessionUpdate)

## Next
- CI: `--install-deps -y` smoke on three OSes
- Interactive y/N for package manager when not -y
- Re-verify SHA-256 if upstream re-uploads

## Blocked
- (none)
