# AriaSharp

Cross-platform .NET 10 Native AOT console app that wraps [aria2](https://aria2.github.io/) (HTTP/HTTPS/FTP/SFTP/magnet) with a purple-gradient Spectre.Console UI.

## Features

- Smart inputs: clipboard, URL args, or URL list files
- Full-screen dashboard (progress, ETA, pause/resume keys) plus plain/inline modes
- Help pager with themed panels (`-h`); `--doctor` diagnostics
- Session resume via `resume.session` in the output directory (saved after each result and on settle)
- User-local aria2 install: package manager when available, else pinned GitHub artifact / TLS source build (no sudo required for the fallback)

## Projects

| Project | Description |
|---------|-------------|
| `AriaSharp.Models` | Models, contracts, defaults |
| `AriaSharp.Services` | aria2 engine, RPC, installer, session |
| `AriaSharp.Console` | CLI, TUI, composition root |
| `AriaSharp.Tests` | xUnit suite (~196 tests) |

## Installation

Prebuilt Native AOT binaries (no .NET runtime needed) are published on the [Releases](https://github.com/migueljocode/AriaSharp/releases) page for Linux (x64, arm64; glibc), macOS (Apple silicon, Intel) and Windows (x64).

### Linux and macOS

```bash
curl -fsSL https://raw.githubusercontent.com/migueljocode/AriaSharp/main/scripts/install.sh | bash
```

Installs `ariasharp` to `~/.local/bin` (no sudo) and tells you if that directory is not on your `PATH`.

### Windows (PowerShell)

```powershell
irm https://raw.githubusercontent.com/migueljocode/AriaSharp/main/scripts/install.ps1 | iex
```

Installs `ariasharp.exe` to `%LOCALAPPDATA%\Programs\AriaSharp` and adds it to your user `PATH`.

### Options

| Variable | Default | Meaning |
|----------|---------|---------|
| `ARIASHARP_VERSION` | `latest` | Release tag to install, e.g. `v1.0.0` |
| `ARIASHARP_INSTALL_DIR` | `~/.local/bin` (Windows: `%LOCALAPPDATA%\Programs\AriaSharp`) | Target directory |

```bash
curl -fsSL https://raw.githubusercontent.com/migueljocode/AriaSharp/main/scripts/install.sh | ARIASHARP_VERSION=v1.0.0 bash
```

### After installing

```bash
ariasharp --install-deps -y   # installs aria2 if it is missing
ariasharp --doctor
```

### Verifying a download

Both installers check the archive against the release's `SHA256SUMS`. That file comes from the same release, so it catches corruption, not a compromised release. For provenance, verify the build attestation:

```bash
gh attestation verify ariasharp-linux-x64.tar.gz --repo migueljocode/AriaSharp
```

Binaries are not code-signed, so a browser download may trigger Gatekeeper (macOS) or SmartScreen (Windows). On macOS, files fetched with `curl` (as `install.sh` does) are not quarantined.

## Requirements

- aria2 1.37.0 (auto-installable via `--install-deps` / `-y`)
- [.NET 10 SDK](https://dotnet.microsoft.com/download) only to build from source (see `global.json`)

## Build / test

```bash
dotnet build src/AriaSharp.slnx
dotnet test src/AriaSharp.slnx
```

## Run

```bash
dotnet run --project src/AriaSharp.Console -- --help
# or after publish:
./ariasharp --doctor
./ariasharp --install-deps -y
./ariasharp https://example.com/file.bin
```

## Publish (Native AOT)

```bash
dotnet publish src/AriaSharp.Console -c Release -r linux-x64
```

## Configuration

Layered: defaults → `~/.config/ariasharp/settings.json` → `ARIASHARP_*` / `NO_COLOR` → CLI.

| Option | Description |
|--------|-------------|
| `-y` / `--install-deps` | Install aria2 when missing (consent when interactive) |
| `--doctor` | Network, managers, aria2 path / HTTPS |
| `-j` | Concurrent downloads (default 2) |
| `-c` / `--connections` | Connections per server, also used for split (default 16); fine-tune with `--split`, `--per-server`, `--min-split-size` (default 1M) |
| `-o` / `--output` | Output directory |
| `-s` / `--session` | Session file or directory; bare `-s` finds the session automatically (default `resume.session` under output dir) |
| `--from-clipboard` / `--from-file` | Force input source |
| `-h` / `-V` | Help / version |

See `docs/AriaSharp Agent Brief (working prompt).md` and `AGENTS.md`.

## License

[MIT](LICENSE)
