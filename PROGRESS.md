# PROGRESS.md

## Milestone
fix/exit-input-session — invalid input fails fast, instant R/Q/EXIT, -s resume session, -j default 2.

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

## Next
- E3: CI not yet run (unverified - no CI access from this environment); PR #4 opened

## Blocked
- (none)
