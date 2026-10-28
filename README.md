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

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (see `global.json`)
- aria2 1.37.0 (auto-installable via `--install-deps` / `-y`)

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
| `-x` / `-s` | Connections / split (default 16) |
| `-d` | Output directory |
| `--session` | Session file (default `resume.session` under output dir) |
| `--from-clipboard` / `--from-file` | Force input source |
| `-h` / `-V` | Help / version |

See `docs/AriaSharp Agent Brief (working prompt).md` and `AGENTS.md`.

## License

[MIT](LICENSE)
