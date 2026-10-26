# PROGRESS.md

## Milestone
M0 Stabilize (S) — reproduce and fix the red CI; clean csproj files, packages and NoWarn; remove duplicate types; fix namespaces; add LICENSE; help exits 0; correct ErrorCatalog with tests. Exit: CI green on three OSes with an honest warning policy.

## Done
- M0 Stabilize committed on main (f9d281a)
- CI test step fixed (no --no-build); Program stub exits 1 with "engine not implemented yet" + test (80fa29e)
- slnx migration committed (328292b): AriaSharp.slnx, deleted AriaSharp.sln, docs/decisions/slnx.md; build/test green on slnx
- graphifyy installed (pipx); graphify update . ran (655 nodes, 1204 edges)
- .graphifyignore + .gitignore updated (graphify-out/ excluded, not committed)
- AGENTS.md created (Project/Rules/Session start/File IO standard/graphify sections); graphify query verified; .codex hooks removed (kilocode approach, not codex)

## Next
- Step 3: efficient file handling (RunWorkspace, SessionFile, settings atomic write, 0600 secret, streaming input, [GeneratedRegex], SearchValues)
- Step 4: hygiene (prune Directory.Packages.props, remove NoWarn entries, CI AOT verify)
- Step 5: open PR (4-5 commits)
- M1 spikes S1-S6 thereafter

## Blocked
- CI macOS/windows status unverified (cannot run locally; pushed to GitHub Actions)

## Decisions
- Namespace: AriaSharp.App (docs/decisions/namespace.md)
- CLI letters: -n = no-install; --no-color long-only
- slnx format with Build Project=true for all 4 projects
- graphify: AST-only, graphify-out/ not committed
