# AriaSharp Agent Brief (working prompt)

Standing instructions for the coding agent working on the AriaSharp repository (GitHub: migueljocode/AriaSharp, branch main). Read all of it before changing anything. Re-read sections 3 and 16 before every commit.

## 1. Mission

Build AriaSharp: a cross-platform .NET 10 / C# 14 terminal application, published as Native AOT, that makes aria2 effortless. The user passes, pipes or copies links, watches a polished full-screen purple-gradient dashboard, and can resume anything that was interrupted. It replaces the owner's dneat scripts (repo migueljocode/WorkWithIO: Bash/dneat, Zsh/dneat.zsh, PowerShell/dneat.ps1) with one codebase. The dneat scripts are the behavioural baseline. Read all three before starting milestone M2.

## 2. Current state (audited; do not trust the README)

- The solution builds in CI, but the CI step Run tests is red on main. First job: reproduce locally and fix it.
- The app does nothing. Program only constructs stubs and exits 0. `Aria2Engine` returns empty data and ignores every call. `DownloadSession` reports success when idle. Spectre.Console, Polly, Microsoft.Extensions.\* and Aria2.JsonRpcClient are unused. The views, pager, signal and screen code are never called.
- 37 tests exist, but all only touch Models. Nothing tests Services or Console.

Confirmed defects (fix each with a test):

- `ErrorCatalog` maps aria2 error codes wrongly (only 13 is right) and the tests lock in the wrong mapping.
- `Aria2ArgsBuilder` writes TRUE and FALSE (aria2 accepts only lowercase true or false and aborts otherwise). It passes `--aria2-opt k=v` as a literal argument (not an aria2 option). It puts the secret on the command line. It ignores the SaveSession setting.
- Temp conf and input files are created with default permissions and never deleted.
- `PrerequisiteInstaller.RunAsync` never drains stdout or stderr, so `make` would hang. The URLs are wrong: the real Windows asset is aria2-1.37.0-win-64bit-build1.zip, the release tag is release-1.37.0, and the aria2.github.io tarball URL is not an official release asset. There are no hashes, no sudo handling, and only apt is supported.
- `SettingsMerger` cannot override a value back to its default and ORs booleans, so the CLI can never switch something off. `SettingsStore` uses reflection JSON (breaks under trimming and AOT) and hardcodes \~/.config.
- `UrlExtractor` requires the characters :// so magnet:? links are never found. `Sanitize` replaces & ' " < > with ? (corrupting URLs) but does not escape Spectre brackets. `MaskCredentials` mis-masks an @ inside a path.
- `SessionStore` counts every non-blank session line, so indented option lines inflate the count.
- `ProgressCalculator` drops completed items from the totals, double counts paused items, keeps hidden state and has unused parameters.
- `ArgParser` has no validation, no `--opt=value`, no `--` terminator, no jobs, connections, split or refresh options. `-f` adds a file path as if it were a URL. Its letters conflict with dneat.
- `--help` exits with code 2 (must be 0). `Terminal.Truncate` throws when max is under 3. The logo does not spell ARIASHARP.
- Duplicate types: `IDownloadSession`, `IAria2Locator` and `IPrerequisiteInstaller` exist in both Models and Services, and `DownloadSnapshot` duplicates `EngineSnapshot`. Files declare namespace AriaSharp.Console while RootNamespace is AriaSharp.App.
- `Directory.Build.props` NoWarn hides IL2026, IL3050 and about 30 other diagnostics. Projects reference unused packages (including the Windows-only EventLog provider), Models references a logging package, the Console csproj has two AssemblyName entries, and the README mentions a LICENSE file that does not exist.

## 3. Rules (non-negotiable)

1. **Architecture:** Console -> Services -> Models, and Tests -> all. Models has no packages and no IO. Services never references Spectre. Only `Aria2Engine` touches Aria2.JsonRpcClient. Dependency injection exists only in Program (the composition root). Enforce this with an architecture test that inspects referenced assemblies.
2. **Clean code (the owner's standing preference):** KISS, DRY, SRP, separation of concerns. Small files with one reason to change. No duplicate type definitions. Every default is written once, in `AppDefaults`. One `OptionTable` drives the parser, help, validation and config keys.
3. **No fake success:** unfinished functionality throws NotImplementedException or returns an explicit failure. Never ship a stub that reports success.
4. **No warning suppression to get green:** shrink NoWarn to the minimum with a comment per entry. IL2026 and IL3050 are never suppressed. Enable PublishAot on the Console project.
5. **Tests with code:** every behaviour change ships with tests. Never edit a test to match wrong behaviour. Verify aria2 facts against the aria2 1.37.0 source or manual, not memory.
6. **Security:** RPC listens on loopback only. The secret lives only in an owner-only conf file (never on a command line or in logs). Mask URL credentials, replace control characters, escape Spectre markup. The aria2 passthrough option must reject security-critical keys: rpc-*, enable-rpc, conf-path, no-conf, stop-with-process, input-file, save-session and every on-* command hook. Every downloaded artifact is HTTPS-only with a pinned SHA-256. Never handle a password. Never write tokens or credentials into files, commits, logs or git remote URLs.
7. **Git:** one feature branch per milestone, small conventional commits, pull request to main, CI green before merge, never force-push main.
8. **Cross-platform:** Linux, macOS and Windows behave correctly. Use Path APIs, no bash-isms, and run CI on all three.
9. **Scope:** items under Later stay later. If something is ambiguous, state the assumption in the report instead of expanding scope.

## 4. Verified facts (use these; mark anything else as unverified)

- aria2 error codes (error\_code.h, 1.37.0): 1 unknown error; 2 timeout; 3 resource not found (HTTP 404 and similar); 4 max-file-not-found; 5 download too slow; 6 network problem; 7 unfinished (in progress); 8 cannot resume; 9 not enough disk space; 10 piece length changed; 11 duplicate download; 12 duplicate info hash; 13 file already exists (treat as Skipped); 14 file renaming failed; 15 file open error; 16 file create error; 17 file I/O error; 18 directory create error; 19 name resolution failed; 20 metalink parse error; 21 FTP protocol error; 22 HTTP protocol error; 23 too many redirects; 24 HTTP authentication failed; 25 bencode parse error; 26 BitTorrent parse error; 27 magnet parse error; 28 option error; 29 HTTP service unavailable; 30 JSON parse error; 31 removed by user; 32 checksum error.
- Boolean options must be lowercase true or false.
- `--save-session-interval` takes seconds only. The session file holds unfinished and errored downloads and is also written on exit. Use the `aria2.saveSession` RPC after each result.
- `--max-connection-per-server` accepts 1 to 16. `--split` defaults to 5. `--min-split-size` defaults to 20M. `--stop-with-process=PID` makes aria2 exit when that process dies.
- RPC: HTTP and WebSocket share one port. Notifications (onDownloadStart, Pause, Stop, Complete, Error, onBtDownloadComplete) arrive only over WebSocket. Useful methods: addUri, tellActive, tellWaiting, tellStopped, tellStatus, getGlobalStat, getServers, getUris, pause, unpause, pauseAll, unpauseAll, removeDownloadResult, saveSession, shutdown, forceShutdown, getVersion, system.multicall. GIDs are 16-digit hex strings, not numbers.
- Official aria2 release 1.37.0 (tag release-1.37.0, Nov 2023) assets: aria2-1.37.0-aarch64-linux-android-build1.zip, aria2-1.37.0-win-32bit-build1.zip, aria2-1.37.0-win-64bit-build1.zip, and aria2-1.37.0.tar.gz, .tar.xz, .tar.bz2. There is no Linux or macOS binary and no checksum file, so download once and pin the SHA-256 yourself in aria2-manifest.json.
- Aria2.JsonRpcClient 1.0.0 (April 2025): targets netstandard2.0, net8 and net9; depends on Microsoft.Extensions.Http, Options, Polly and System.Text.Json; registered through dependency injection; notification events need the WebSocket connection with ReceiveNotifications enabled. Native AOT compatibility is not declared (spike S1).
- Spectre.Console has `AnsiConsole.AlternateScreen`, Live and Layout. Spectre.Console.Cli is not AOT-safe: do not use it. Only one live widget at a time. Spectre.Console.Testing provides TestConsole.
- Loopback RPC clients must bypass proxies (HttpClient handler UseProxy = false). On Windows aria2 may also receive Ctrl-C.

## 5. Owner requirements

- A separate xUnit Tests project.
- Use the NuGet package Aria2.JsonRpcClient, leveraging its notification events, behind our own `IDownloadEngine`. If spike S1 shows it is not AOT-safe, write a thin fallback client on ClientWebSocket and HttpClient with source-generated System.Text.Json, and tell the owner.
- Changeable defaults: max-concurrent-downloads 1, split 16, max-connection-per-server 16, auto-file-renaming false, save-session to a resume file, saved after each download result (saveSession on events; keep a 60 s interval as a crash net).
- The session file lives in the output directory (default: current directory) and is named resume.session.
- Show total ETA for all downloads and the protocol used per download.
- If no package manager or trustworthy binary is available, download and extract (Windows zip) or compile (Linux and macOS source tarball) from the official aria2 release page.
- Console refresh rate configurable, default 500 ms.
- `-h` and `--help`: comprehensive, colorful, stunning, purple gradient, taking the whole console like less. The download view gets the same full-screen treatment.
- Leverage the strengths of dneat (section 11 and the dneat parity list in section 14).
- Native AOT, .NET 10, C# 14, Spectre.Console used directly, a lightweight custom argument parser, one self-contained native executable per OS and CPU, aria2 stays a separate executable, and no aria2 binaries are committed.

## 6. Target architecture

- **AriaSharp.Models** (pure): immutable `DownloadItem` (GID string, name, state, protocol and effective protocol, sizes, speed, connections, masked URI, error), `DownloadState`, `DownloadProtocol`, `EtaEstimate` with confidence, `SessionState`, `RunOutcome`, `ExitCode`, `AppDefaults`, immutable `AppSettings` (persisted keys only), `SettingsOverlay` (nullable fields for layering), `Aria2ErrorInfo` and a correct `ErrorCatalog`, one definition of each contract (`IDownloadEngine`, `IDownloadSession`, `IAria2Locator`, `IPrerequisiteInstaller`, `ISettingsStore`), and formatting extension helpers.
- **AriaSharp.Services** (all logic): `Aria2Locator`, `Aria2Process`, `Aria2ArgsBuilder`, `Aria2Engine` (library wrapper plus mapping to Models), `DownloadSession`, `ProgressCalculator`, `SessionStore`, Input (`UrlExtractor`, `UrlClassifier`, `UrlSanitizer`, `UrlNameGuesser`), Setup (`PackageManagerCatalog`, `PrerequisiteInstaller`, `Aria2SourceBuilder`, `Aria2Manifest`, `FailureClassifier`, `NetworkProbe`), Config (`SettingsStore`, `SettingsMerger`).
- **AriaSharp.Console** (everything the user sees): `Program` (composition root), Cli (`OptionTable`, `ArgParser`, `HelpDocument`), input readers (args, files, stdin, per-OS clipboard), Ui (`Theme`, `Gradient`, widgets, `Pager`, `DashboardView`, `InlineView`, `PlainView`, `SummaryView`, `Prompts`), Terminal (`TerminalCapabilities`, `ScreenGuard`, `TtyKeyReader`, `SignalHandler`). Namespace must match RootNamespace and must not be named Console.
- **AriaSharp.Tests**: Unit, Snapshots (Spectre TestConsole), Integration (real aria2 plus a local HTTP server), Architecture.
- aria2 is the source of truth. AriaSharp keeps no queue of its own: Services projects aria2 state into models and the UI renders them.

## 7. aria2 integration spec

One managed aria2 process per run; never adopt a foreign daemon.

Startup arguments: `--enable-rpc`, `--rpc-listen-port` (free loopback port: bind port 0, release), `--rpc-listen-all=false`, `--conf-path` (owner-only temp file with `rpc-secret`, a random 128-bit value, deleted after startup; never pass `--rpc-secret`), `--stop-with-process=<our PID>`, `--input-file` (private temp copy of all inputs, never the session file itself), `--dir`, `--max-concurrent-downloads`, `--split`, `--max-connection-per-server`, `--auto-file-renaming`, `--allow-overwrite` (true only with force), `--save-session=<output dir>/resume.session`, `--save-session-interval=60`, optional `--max-overall-download-limit`, and quiet flags (`--summary-interval=0 --show-console-readout=false --console-log-level=warn --enable-color=false --download-result=hide`). Booleans lowercase. Extra `--aria2-opt key=value` pairs become `--key=value` after validation against the blocked-key list.

Owner-only files: on Unix create with UnixCreateMode user read and write; on Windows rely on the per-user temp directory. Delete temp files on every exit path.

Lifecycle:

1. Locate aria2, create temp files, start the process, wait up to 10 s for RPC readiness (clear error on timeout), connect, read the version.
2. Notifications update item state instantly. On complete, error and stop, call `aria2.saveSession`. One `system.multicall` (tellActive plus getGlobalStat) per poll tick feeds progress. The poll interval follows the refresh interval with a 250 ms floor. Waiting and stopped lists are fetched at startup and on events. If the WebSocket drops, reconnect with backoff and poll tellStopped meanwhile.
3. Record per-item start and end times from events, so per-file time and speed work with parallel jobs.
4. Idle: numActive and numWaiting both 0 on two consecutive polls (one poll if every queued item has reported).
5. Settle: saveSession, then `aria2.shutdown` with a 3 s grace period, then kill. Remove skipped (code 13) results with removeDownloadResult before the final save (verify in spike S3). Count only URL lines in the session file, delete the file when empty, otherwise print the resume hint.
6. Interrupt: first Ctrl-C, SIGTERM or SIGHUP requests a graceful stop; the second kills at once. Exit 130, terminal restored, resume hint printed. aria2 may receive the same signal, so tolerate it.
7. Always drain aria2 stdout and stderr to a log file. The log never contains the secret or URL credentials.

Session rules: default resume.session in the output directory, changeable. At startup, if the file holds entries and is not the thing being resumed: on a TTY prompt Resume, Replace or Abort (default Resume); non-interactive warns and replaces (dneat behaviour). Resume with `ariasharp resume.session` or `--resume`; aria2 stores each download's directory, so the output option is not needed. An existing file gives code 13: shown as skipped, never as unfinished.

## 8. Input spec

Sources in order: positional arguments and `-i/--input` (repeatable), each classified as a URL (http, https, ftp, ftps, sftp, magnet) or an existing file (aria2 list file or session file); then stdin when piped and nothing else was given; then the clipboard when interactive and nothing else was given (list the URLs in the Plan card and confirm unless `--yes`); otherwise a friendly usage hint, with a paste prompt on a TTY. Merge everything into one private temp file with CRLF stripped. List files pass through untouched so aria2 syntax (indented out= and dir= options, tab-separated mirrors) keeps working; count only lines that start with a URL scheme. Extraction pattern follows dneat: scheme list including magnet:? and trailing punctuation trimmed. `UrlSanitizer` shows control characters as ?, masks credentials only in the authority part, and Spectre markup is escaped at render time (do not corrupt the URL used for the download). When stdin is a pipe, read hotkeys from /dev/tty (Unix) or CONIN$ (Windows); if unavailable run without hotkeys. Clipboard readers: Windows Win32 API through LibraryImport, macOS pbpaste, Linux wl-paste, xclip or xsel. After a run, on a TTY only, offer to delete used list files (never the session file) unless keep-list is set.

## 9. Progress and ETA spec

- Per item: percent = completed / total; unknown total shows a spinner and bytes done.
- Overall percent is byte-weighted across all items with a known size, including completed ones. While some sizes are unknown mark it with \~ and blend an item-count estimate. Do not double count paused items.
- Speed: show aria2's raw global speed. Smooth with an EMA (alpha 0.3) for ETA only. Keep calculator state in one place (no unused parameters).
- Total ETA = remaining bytes of known items / smoothed speed, with a confidence marker: exact (all sizes known), >= (some queued sizes unknown), \~ (no sizes: dneat's elapsed-time extrapolation). Show no ETA until speed has been stable for 3 ticks.
- Protocol badge on every row: URL scheme, refreshed after start from getServers so a redirect shows the effective protocol. Badge colours from dneat: HTTPS violet, HTTP amber, FTP and SFTP orange, MAGNET magenta.

## 10. Terminal UI spec

- Capability detection: interactive (stdin and stdout redirection), ANSI, Unicode (locale), colour level (NO\_COLOR, TERM, COLORTERM, WT\_SESSION on top of Spectre's detection), size with resize polling. On Windows enable VT processing and force UTF-8.
- Modes: **Full-screen** (default on a TTY of at least 76 columns by 14 rows): alternate screen buffer using Spectre AlternateScreen, Live and Layout; on exit restore the screen and print the completed list plus a Summary card to the normal buffer. **Inline** (`--inline`, or a smaller terminal): dneat's view, result lines scrolling above two live lines (current file, overall). **Plain** (redirected or `--plain`): one line per event, no escape codes.
- Theme (single file): gradient stops #5B21B6, #7C3AED, #8B5CF6, #A78BFA, #DDD6FE for logo, rules, titles and bars (dark at the start, bright at the head). Fallbacks: a 256-colour violet ramp (for example 54, 91, 98, 99, 135, 141, 147, 183, 189), magenta at 16 colours, nothing under NO\_COLOR. Semantic colours as in dneat (ok green, warn amber, error red, muted grey). `Gradient` samples a colour at t in 0 to 1 and paints text per grapheme or bars per cell without allocating in the render loop. Logo: block-letter ARIASHARP is about 80 columns, so use a stacked two-row variant under 90 columns and plain gradient text under 52.
- Dashboard: header, stats row (total progress bar, speed, total ETA with confidence, counts of active, queued, done, failed, skipped, elapsed), scrollable queue table (status, number, name, protocol badge, size, progress, speed, ETA), detail line for the selected item (masked URL, connections, GID), key bar. Breakpoints reuse dneat's 100 and 76 columns. Keys: q or Ctrl-C graceful stop, p pause or resume all, arrows, PgUp, PgDn, g, G navigate, Enter details, r retry failed, a add links from the clipboard (addUri), l log panel, ? help overlay, Esc closes overlays. Render tick = refresh option (default 500 ms, range 100 to 5000); redraw immediately on events; no redraw when nothing changed.
- Help pager (`-h`, `--help`): same screen mechanics. Content generated from `OptionTable` (never hand-duplicated): gradient logo, tagline, cards USAGE, INPUT & OUTPUT, TRANSFER, DISPLAY, SETUP, CONFIG (files, environment, precedence), KEYS, EXAMPLES, GOOD TO KNOW, EXIT CODES; defaults read from `AppDefaults`. less-style keys: j k and arrows, Space and b, PgUp and PgDn, d and u, g and G, / search with n and N, q or Esc to quit, status line like 'lines 1-38 of 142 27%'. Reflow on resize. Not a TTY or `--no-pager`: plain grep-friendly text. Exit code 0.
- Terminal safety: `ScreenGuard` restores cursor, alternate screen and input mode on every exit path (finally, unhandled exception, CancelKeyPress, PosixSignalRegistration for SIGTERM and SIGHUP).

## 11. CLI and configuration spec

- Letters follow dneat: `-i/--input`, `-o/--output` (default .), `-f/--force`, `-k/--keep-list`, `-j/--jobs` (1 to 16, default 1), `-c/--connections` (1 to 16, default 16, sets split and per-server), `-l/--limit` (500K or 2M; decimals converted, 1.5M = 1536K), `-s/--session` (default resume.session in the output directory), `-y/--install-deps`, `-n/--no-install` (wins over -y), `-h/--help`, `-V/--version`. Additions: `--split`, `--per-server`, `--min-split-size`, `--aria2-opt key=value` (repeatable), `--aria2-path`, `--refresh <ms>` (default 500, 100 to 5000), `--poll <ms>`, `--inline`, `--plain`, `--no-pager`, `--no-color`, `--ascii`, `--resume`, `--yes`, `--config`, `--print-config`, `-v/--verbose`. Flag any letter change to the owner.
- Parser: supports `--opt value`, `--opt=value`, short clusters and `--`; errors name the option and end with a hint to run --help (dneat wording, for example 'Jobs must be a number from 1 to 16'). Missing values are errors. The `OptionTable` is the only definition of options.
- Config: JSON in the platform config folder (SpecialFolder.ApplicationData), source-generated serializer, camelCase keys. Precedence: CLI, environment, config file, built-in defaults, implemented with `SettingsOverlay` (unset is distinct from default). Environment: NO\_COLOR, ARIASHARP\_NETWORK (online or offline), ARIASHARP\_ASCII, ARIASHARP\_CONFIG, ARIASHARP\_MIRROR, standard proxy variables.
- Exit codes: 0 success, 1 a download or run failed, 2 usage error, 130 interrupted.

## 12. Prerequisites and installers spec

- `Aria2Locator` order: explicit path, PATH, known folders (Linux /usr/bin, /usr/local/bin, \~/.local/bin; macOS /opt/homebrew/bin, /usr/local/bin; Windows WinGet Links, Chocolatey bin, Scoop shims), then AriaSharp's own tools directory. After an install, re-probe by absolute path.
- `PackageManagerCatalog` is data-driven: apt-get, dnf, pacman, zypper, Homebrew, winget (id aria2.aria2), choco, scoop. Use root or sudo through the normal prompt, show the exact command first, never handle a password, capture output to a log, classify failures (network, package missing, manager busy, authentication, other) and print a manual hint per manager. Ask y/N before installing unless `--yes`; `-n` forbids installs.
- Fallbacks: Windows without a package manager downloads the official win-64bit (or win-32bit) zip into the user tools directory. Linux and macOS compile the official aria2-1.37.0.tar.gz (System.Formats.Tar): check the compiler, make, TLS and zlib headers first and print the exact per-distribution package list if missing, configure with the smallest dependency set, `make -j`, install into the user tools directory, verify `aria2c --version` and HTTPS support. Always drain process output, build in a private temp directory, ask before starting, and pin the SHA-256 for every artifact.
- Network probe: TCP 443 to 1.1.1.1 and 8.8.8.8, then DNS for github.com; proxy variables count as online; ARIASHARP\_NETWORK overrides.
- `install.sh` and `install.ps1` are thin: detect OS and CPU, download the release artifact and SHA256SUMS (honour ARIASHARP\_MIRROR and proxies), verify, install to a user directory without sudo, then run `ariasharp --install-deps`.

## 13. Build, AOT and CI

- Directory.Build.props: net10.0, C# latest, nullable, implicit usings, warnings as errors, analyzers, and a justified minimal NoWarn. Console project: PublishAot, InvariantGlobalization, one AssemblyName, no trimall single-file settings. Remove unused packages. Central package management lists only used packages.
- CI on ubuntu, macos and windows: restore, build, unit, snapshot and architecture tests, integration tests with real aria2, AOT publish per RID (linux-x64, linux-arm64, osx-arm64, osx-x64, win-x64), smoke tests on the published binary (--version, non-TTY --help, one real download from a local server, Ctrl-C then resume), size and start-up budgets recorded after the first AOT build, release on tag with SHA256SUMS and artifact attestations.
- Add the LICENSE file (MIT, as the README says).

## 14. Testing requirements

- Unit: ArgParser (table-driven), UrlExtractor, UrlClassifier, UrlSanitizer, ProgressCalculator, SessionStore, Aria2ArgsBuilder (assert lowercase booleans, no secret in args, blocked keys rejected), ErrorCatalog (all 32 codes), FailureClassifier, PackageManagerCatalog, SettingsMerger (layering and reset-to-default), Gradient, Pager viewport and search.
- Snapshots: every view in Spectre TestConsole at widths 60, 80, 100, 140 across colour none, 16, 256 and truecolor, Unicode and ASCII.
- Fakes: a FakeDownloadEngine plays scripted scenarios (success, 404, exists, pause, event floods) for DownloadSession and the views.
- Integration (category Integration): real aria2 plus a local HTTP server (ranges, redirects, 404, throttling): notifications, saveSession contents, resume after kill, skip-exists, graceful stop, and orphan prevention (kill AriaSharp and aria2 exits).
- Architecture: dependency rules from section 3.
- dneat parity cases: URL extraction and trailing punctuation, argument classification, list files with CRLF, limit conversion, validation messages, skip semantic (code 13), stale session warning, session deleted when empty, resume hint, exit codes, Plan card rows (Source, Output, Engine, Transfer, Existing, Resume), Summary card rows (Result, Size, Speed, Time, Output, Resume), list-removal prompt.
- Rule: every bug fix gets a regression test; aim for 80 percent coverage on Models and Services.

## 15. Milestones

- **M0 Stabilize (S):** reproduce and fix the red CI; clean csproj files, packages and NoWarn; remove duplicate types; fix namespaces; add LICENSE; help exits 0; correct `ErrorCatalog` with tests. Exit: CI green on three OSes with an honest warning policy.
- **M1 Spikes (S):** S1 Aria2.JsonRpcClient under PublishAot (add, status, WebSocket notifications, multicall). S2 Spectre Live plus Layout plus AlternateScreen plus non-blocking keys on Linux, macOS, Windows (AOT). S3 aria2 behaviour: conf-path secret and ignoring the user's aria2.conf, saveSession contents with removeDownloadResult, shutdown timing, stop-with-process on each OS, notification ordering, input-file plus RPC, ftps support. S4 Ctrl-C and process groups per OS. S5 minimal source build on Ubuntu 24.04 and macOS. S6 clipboard readers under AOT. Exit: one decision note per spike in docs/decisions.
- **M2 Engine (M):** locator, process, conf and secret, args builder, `Aria2Engine`, events and polling, shutdown. Exit: an integration test starts aria2, adds three URLs (ok, 404, exists), receives events, saves the session and stops gracefully.
- **M3 Session, input, CLI, config (M):** `ProgressCalculator`, `SessionStore`, `DownloadSession`, input classes and readers, `OptionTable` and `ArgParser`, config layering, plain view, exit codes. Exit: the dneat parity tests pass and `ariasharp urls.txt --plain` downloads real files and resumes after a kill.
- **M4 Terminal UI (L):** theme, gradient, widgets, inline view, summary, dashboard, pager and help, key handling, capability fallbacks, snapshots. Exit: correct at every breakpoint, no flicker, terminal always restored (fault-injection test), help generated from `OptionTable`.
- **M5 Prerequisites and installers (M):** catalog, installer, zip and source fallbacks, manifest with pinned hashes, install scripts. Exit: clean installs verified on Ubuntu, Fedora, Arch, openSUSE, macOS (with and without Homebrew) and Windows (with and without winget).
- **M6 AOT and release (M):** matrix publish, smoke tests, attestations, budgets, README. Exit: a tagged release produces tested binaries for every RID.
- **M7 Hardening and docs (S):** profiling, parser fuzzing, accessibility, `ariasharp doctor`, troubleshooting guide.
- **Later:** GUI on Services, JSON event output, scheduling, winget, Homebrew and Scoop packages, optional `--probe-sizes`.

## 16. Working protocol and reporting

- Start every session by reading this brief, the git log and PROGRESS.md. Keep PROGRESS.md current (milestone, done, next, blockers, decisions).
- Work in small vertical slices and keep the build green. Run dotnet build, dotnet test and (from M6) the AOT publish before every commit and report the real results. If you cannot run something, say so instead of claiming it passes.
- Definition of done for a task: code follows section 3, tests added and passing, no new warnings, docs or help updated, behaviour verified (run it, do not assume).
- After each milestone report: what changed, tests added, verification commands with results, what you could not verify, assumptions, and decisions needed from the owner.
- Ask the owner only for the open decisions below or for real blockers. Otherwise decide, record the decision in docs/decisions, and continue.

## 17. Open decisions for the owner

1. Keep a dneat alias for the ariasharp command?
2. Namespace for the Console project (AriaSharp.App or AriaSharp.Cli)?
3. Default render mode: full-screen (this brief) or inline with an opt-in flag?
4. v1 platform scope: all three OSes, or Linux first?
5. macOS signing and notarization budget.
