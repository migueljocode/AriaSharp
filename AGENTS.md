# AriaSharp — AGENTS.md

## Project

AriaSharp is a .NET 10 console application wrapping the aria2 download engine (HTTP/HTTPS/FTP/SFTP/magnet). It has three library projects under `src/` (`AriaSharp.Models`, `AriaSharp.Services`, `AriaSharp.Console`) and a test project under `tests/` (`AriaSharp.Tests`). The full spec is `docs/AriaSharp Agent Brief (working prompt).md` — read it before making design decisions.

## Rules

### Architecture rules (section 7 / Brief)
- Contracts live in `AriaSharp.Models` (`Contracts.cs`), never duplicated in `Services`.
- Types are `internal` by default in the Console (exe) project; public only when a test or another project needs them.
- No namespace named `Console`; namespace must match `RootNamespace` (`AriaSharp.App` for Console).
- `PublishAot=true` on the Console project; never suppress `IL2026` or `IL3050`.

### Honesty rules (section 3 / Brief)
- Never ship a stub that pretends to work. Until M2, `Program` exits 1 with "engine not implemented yet" when given inputs — do not fake aria2 execution.
- Report real build/test output; do not claim green if red.

### Security rules
- Secrets (aria2 RPC secret, URL credentials) never on the command line; owner-only secret files (`0600` on Unix).
- No secrets or keys logged or committed.
- Credentials masked before any output/log write.

### Session start (2-step routine)
1. Read the Brief (`docs/AriaSharp Agent Brief (working prompt).md`), `PROGRESS.md`, and `git log`.
2. Run `dotnet build -c Release` and `dotnet test -c Release`; report real output before making changes.

### File IO standard (step 3, condensed)
- Streaming only: `StreamReader`/`StreamWriter { NewLine = "\n" }`, `FileOptions.Asynchronous | SequentialScan`, no whole-file buffering, no intermediate `string`/`StringBuilder`.
- No loose temp files: `RunWorkspace` (`Directory.CreateTempSubdirectory("ariasharp-")`, owner-only on Unix, `IAsyncDisposable`, recursive delete in `finally`).
- Secret files: `FileStreamOptions { Mode = CreateNew, Access = Write, Share = None, UnixCreateMode = UserRead|UserWrite }` → mode 0600.
- Paths: `Path.Join`, not `Path.Combine`; `OperatingSystem.IsWindows()`, not `Environment.OSVersion.Platform`.
- Atomic settings writes: temp file in same dir + `File.Move(tmp, path, overwrite: true)`.
- Regex: `[GeneratedRegex]` (not `new Regex(..., Compiled)`).
- Schemes/trailing punctuation: `SearchValues<char>` / `SearchValues<string>` (OrdinalIgnoreCase); `ReadOnlySpan<char>` + `MemoryExtensions`.
- List-file detection: only after a URL line or on recognised aria2 option keys (`out`, `dir`, …), not any line containing `=`.

## graphify

This project has a code knowledge graph at `graphify-out/` (AST-only, no API key, no cost).

Rules:
- Run `graphify query "<question>"` before your first grep/search/source-file read for a codebase question. Read raw files only for lines the graph already located.
- Use `graphify path "<A>" "<B>"` for relationships; `graphify explain "<concept>"` for focused concepts.
- `graphify-out/` files are dirty after hooks/incremental updates — that's normal, not a reason to skip graphify.
- After modifying code, run `graphify update .` to keep the graph current.
- A committed `graphify-out/graph.json` is rewritten whole on every build; a large diff in a PR is expected — do not flag it as churn.

Install: `pipx install graphifyy` (CLI is `graphify`). Add the install line to the README "Contributing" section. `.graphifyignore` excludes `bin/`, `obj/`, `TestResults/`, `graphify-out/`; `graphify-out/` is also in `.gitignore`. Do not commit `graphify-out/`.
