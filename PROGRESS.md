# PROGRESS.md

## Milestone
fix/exit-input-session — invalid input fails fast, instant R/Q/EXIT, -s resume session, -j default 2, plus Issues 1-3 (no / pager search, Connections=16, Pause drops speed to 0).

## Done
- STEP 0: cleanup ForceShutdownAsync indentation/DisposeAsync; OptionTable jobs uses CultureInfo.InvariantCulture
- A1: InputResolver.ExpandArgsAsync rejects non-URL/non-file args, collects rejected args
- A2: InputCommand.ResolveAsync reports rejected args with "Error: no valid URL..." exit 2
- A3: DownloadPaths.Resolve defers Directory.CreateDirectory; DownloadCommand wraps in try/catch with exit 2
- A4: DashboardRunner skips AnsiConsole.Live until first snapshot has items; prints error on normal screen
- A5: tests for invalid input rejection, DownloadPaths no dir creation, InputCommand exit 2
- B1: StopAsync added to IDownloadEngine/IAria2Engine; order: SaveSession(1s) -> ForceShutdown -> Kill(500ms) -> dispose notifications
- B2: DisposeAsync calls StopAsync; ShutdownAsync removed; GracefulStopTimeoutMs removed; Aria2Process uses ForceStopWaitMs
- B3: Aria2NotificationListener.DisposeAsync aborts socket (no close handshake), capped 300ms
- B4: DownloadCommand calls engine.StopAsync before DownloadSummary.FinishAsync
- B5: --verbose prints "engine stopped in <n> ms" to stderr
- B6: forceShutdown session-file write verified against aria2 1.37.0 manual (unverified - no live aria2 to test)
- B7: tests for StopAsync idempotency, no Shutdown call
- C1: OptionKind.OptionalString added; -s / --session takes optional value; bare -s = auto
- C2: SessionLocator in Services/IO; resolves session file/dir, throws SessionNotFoundException if missing
- C3: DownloadPaths.Resolve uses SessionLocator; ARIASHARP_SESSION env/config via existing SettingsOverlay.SessionFile mapping
- C4: -s sets Resume=true, skips "No URLs found", passes session file as extra --input-file to aria2
- C5: SessionLocatorTests (dir value, file value, bare -s found/missing, explicit -o wins)
- D1: AppDefaults.MaxConcurrentDownloads stays 2; precedence CLI > env > config > default unchanged
- D2: README.md line 58 "default 1" -> "default 2"; Brief line 58 "max-concurrent-downloads 1" -> "2"
- D3: SettingsBootstrap diagnostics to stderr when Verbose/PrintConfig: config path, env names, jobs source layer
- D4: JobsDefaultTests (default=2, -j 1 ok, -j 0/17 throw, env beats default, CLI beats env, jobs source)
- Issue 1: Removed /, n, N search from HelpPager; status line `✦ Help {offset+1}–{end}/{total} {label} · j/k space/b g/G q`; removed search wording from HelpView KEYS panel, HelpDocument KEYS section, Brief lines 113/139; deleted ReadSearch/FindNext/dead code
- Issue 2a: Added AppDefaults.MinSplitSize = "1M"
- Issue 2b: Aria2ArgsBuilder emits --min-split-size=1M before ExtraOptions; user override wins
- Issue 2c: Conn column shows {live}/{max} (e.g. 16/16); removed duplicate Split column; DashboardView/DashboardLoop/DashboardRunner threaded with MaxConnectionPerServer; defaults Split=16, MaxConnectionPerServer=16 unchanged
- Issue 3a: P key calls ForcePauseAllAsync (aria2.forcePauseAll); deleted PauseAllAsync; Resume stays unpauseAll
- Issue 3b: ProgressCalculator TotalDownloadSpeed = sum of active items' DownloadSpeed (0 when none); per-item speed 0 for non-active; no globalStat speed

## Next
- Issue 2 Step 1 diagnosis (real 100MB+ download + aria2.getOption) - not run (no live aria2/network)
- Issue 3 Step 3 manual check (time from P keypress to 0 B/s) - not run (no live aria2)
- E3: CI not run (no CI access); PR #4 opened

## Blocked
- (none)

## Round 3 — pause/queue, clock, UI polish
- Done: pause keeps queued items Waiting (`PauseTracker`, thread-safe, additive); elapsed frozen while paused (`RunClock`); paused bar yellow; soft logo-style bar gradient (`Gradient.Soft`); Split column restored; `Connection` header; centered headers, ETA/Split/Connection values centered, Progress/Speed values right-aligned.
- Verified by CI (Actions, ubuntu/macos/windows): build, unit tests, AOT publish, smoke tests.
- Not done / unverified: `graphify update` (no tooling here); live check against real aria2 that (a) queued items stay Waiting after P, (b) the same two downloads resume first after R, (c) speed reads 0 right after P, (d) the visual look of the soft gradient and header alignment.
