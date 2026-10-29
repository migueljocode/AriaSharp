#!/usr/bin/env bash
# AriaSharp installer for Linux and macOS.
#   curl -fsSL https://raw.githubusercontent.com/migueljocode/AriaSharp/main/install.sh | bash
# Environment:
#   ARIASHARP_VERSION      release tag to install (default: latest)
#   ARIASHARP_INSTALL_DIR  target directory (default: ~/.local/bin)
set -euo pipefail

REPO="migueljocode/AriaSharp"

die() { printf 'error: %s\n' "$*" >&2; exit 1; }

detect_rid() {
  local os arch ldd_out
  case "$(uname -s)" in
    Linux)  os=linux ;;
    Darwin) os=osx ;;
    *) die "unsupported OS: $(uname -s) (on Windows use install.ps1)" ;;
  esac
  case "$(uname -m)" in
    x86_64|amd64)  arch=x64 ;;
    aarch64|arm64) arch=arm64 ;;
    *) die "unsupported CPU: $(uname -m)" ;;
  esac
  if [ "$os" = linux ]; then
    ldd_out=$(ldd --version 2>&1 || true)
    case "$ldd_out" in
      *musl*) die "musl-based Linux (for example Alpine) is not supported; the release binaries target glibc" ;;
    esac
  fi
  printf '%s-%s' "$os" "$arch"
}

sha256_of() {
  if command -v sha256sum >/dev/null 2>&1; then
    sha256sum "$1" | cut -d' ' -f1
  elif command -v shasum >/dev/null 2>&1; then
    shasum -a 256 "$1" | cut -d' ' -f1
  else
    die "sha256sum or shasum is required"
  fi
}

main() {
  command -v curl >/dev/null 2>&1 || die "curl is required"
  command -v tar >/dev/null 2>&1 || die "tar is required"

  local rid version dest base file expected actual
  rid=$(detect_rid)
  version="${ARIASHARP_VERSION:-latest}"
  dest="${ARIASHARP_INSTALL_DIR:-$HOME/.local/bin}"
  if [ "$version" = latest ]; then
    base="https://github.com/$REPO/releases/latest/download"
  else
    base="https://github.com/$REPO/releases/download/$version"
  fi
  file="ariasharp-$rid.tar.gz"

  tmp=$(mktemp -d)
  trap 'rm -rf "${tmp:-}"' EXIT

  printf 'Installing AriaSharp (%s, %s) to %s\n' "$version" "$rid" "$dest"
  curl -fsSL "$base/$file" -o "$tmp/$file" || die "download failed: $base/$file"
  curl -fsSL "$base/SHA256SUMS" -o "$tmp/SHA256SUMS" || die "download failed: $base/SHA256SUMS"

  expected=$(awk -v f="$file" '$2 == f || $2 == "*" f { print $1 }' "$tmp/SHA256SUMS")
  [ -n "$expected" ] || die "no checksum for $file in SHA256SUMS"
  actual=$(sha256_of "$tmp/$file")
  [ "$expected" = "$actual" ] || die "checksum mismatch for $file"

  tar -xzf "$tmp/$file" -C "$tmp" ariasharp
  mkdir -p "$dest"
  install -m 0755 "$tmp/ariasharp" "$dest/ariasharp"

  printf 'Installed %s\n' "$dest/ariasharp"
  case ":$PATH:" in
    *":$dest:"*) ;;
    *) printf '\n%s is not on your PATH. Add it with:\n  export PATH="%s:%s"\n' "$dest" "$dest" "\$PATH" ;;
  esac
  printf '\nNext: ariasharp --install-deps -y   # installs aria2 if it is missing\n'
}

main "$@"
