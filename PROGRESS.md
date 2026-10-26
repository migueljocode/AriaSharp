# PROGRESS.md

## Milestone
M0 Stabilize (S) — reproduce and fix the red CI; clean csproj files, packages and NoWarn; remove duplicate types; fix namespaces; add LICENSE; help exits 0; correct ErrorCatalog with tests. Exit: CI green on three OSes with an honest warning policy.

## Done
- M0 Stabilize committed on main (f9d281a)
- CI test step fixed (no --no-build); Program stub exits 1 with "engine not implemented yet" + test (80fa29e)
- slnx migration committed (328292b): AriaSharp.slnx, deleted AriaSharp.sln, docs/decisions/slnx.md; build/test green on slnx
- graphifyy installed (pipx); graphify update . ran (697 nodes, 1306 edges after Step 3)
- .graphifyignore + .gitignore updated (graphify-out/ excluded, not committed)
- AGENTS.md created (Project/Rules/Session start/File IO standard/graphify sections); graphify query verified; .codex hooks removed (kilocode approach, not codex)
- Step 3 file handling rework committed:
  - RunWorkspace: Directory.CreateTempSubdirectory("ariasharp-"), IAsyncDisposable, recursive delete in DisposeAsync, Path.Join for InputFile/ConfFile/LogFile
  - SessionFile: static class with IAsyncEnumerable<string> EnumerateEntriesAsync (FileShare.ReadWrite|Delete), HasEntries short-circuit, Delete (sync) + DeleteAsync, IsAria2ListFile (URL line OR recognised option key only)
  - InputMerger: streaming StreamWriter { NewLine = "\n" }, FileOptions.Asynchronous | SequentialScan, no StringBuilder
  - SettingsStore: AppPaths (XDG_CONFIG_HOME/APPDATA), DeserializeAsync from FileStream, atomic write (tmp + File.Move overwrite), corrupt file → warning + defaults, optional path param for testing
  - Input.cs: [GeneratedRegex], SearchValues<char> for trailing punctuation, SearchValues<string> for schemes, ReadOnlySpan<char>, MemoryExtensions
  - Aria2Process: ChannelWriter<string> logging, LogWriter (AutoFlush, Channel<string>), secret/credential redaction, StringBuilder removed
  - EngineSettings: InputFile/ConfFile replace TempInputFile/TempConfFile
  - AppPaths model class added
  - DownloadSession: ISessionStore dependency removed
  - SessionStore.cs deleted (replaced by RunWorkspace + SessionFile + InputMerger)
- 89 tests pass (77 + 12 FileIoTests), build 0 errors 0 warnings
- CA1822 added to NoWarn with justification (interface delegate overloads)

## Next
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