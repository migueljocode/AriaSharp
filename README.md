# AriaSharp

A cross-platform .NET 10 console application that wraps the [aria2](https://aria2.github.io/) download engine, providing a modern terminal UI for managing downloads.

## Features

- Download files via HTTP, HTTPS, FTP, SFTP, and magnet links
- Interactive terminal dashboard with real-time progress
- Inline, plain, and summary output modes
- Session resume support via aria2 session files
- Configurable download limits, connections, and split settings
- Prerequisite and aria2 installation helpers
- Single-file, self-contained publish for easy distribution

## Projects

| Project | Description |
|---------|-------------|
| `AriaSharp.Models` | Data models, enums, contracts, and formatting helpers |
| `AriaSharp.Services` | aria2 process management, session store, progress calculation, prerequisite installation |
| `AriaSharp.Console` | Terminal UI, argument parsing, views, and program entry point |
| `AriaSharp.Tests` | xUnit test suite (37 tests) |

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (pinned to 10.0.113 via `global.json`)
- [aria2](https://aria2.github.io/) (v1.37.0 recommended)

## Build

```bash
dotnet build AriaSharp.slnx
```

## Test

```bash
dotnet test AriaSharp.slnx
```

## Run

```bash
dotnet run --project src/AriaSharp.Console -- --help
```

## Publish (single-file, self-contained)

```bash
dotnet publish src/AriaSharp.Console -c Release -r linux-x64 --self-contained
```

## Configuration

Settings are stored in `~/.config/ariasharp/settings.json`. Command-line options override config file values.

### Common Options

| Option | Description |
|--------|-------------|
| `-i, --input <url>` | Add URL(s) to download |
| `-f, --file <path>` | Read URLs from a list file |
| `-d, --dir <path>` | Output directory (default: `.`) |
| `-s, --session <path>` | Session file path |
| `-a, --aria2-path <path>` | Path to aria2c binary |
| `-l, --speed-limit <value>` | Max overall download limit |
| `-n, --no-color` | Disable color output |
| `-p, --plain` | Plain text output |
| `-e, --inline` | Inline dashboard mode |
| `-v, --verbose` | Verbose output |
| `-y, --yes` | Assume yes to prompts |
| `-F, --force` | Overwrite existing files |
| `-r, --resume` | Resume incomplete downloads |
| `--install-aria2` | Install aria2 if missing |
| `--install-deps` | Install prerequisites |

## License

[MIT](LICENSE)