#!/usr/bin/env sh
# Downloads the Tailwind CSS v3 standalone CLI for the current OS/arch into this folder.
set -eu
VERSION="${TAILWIND_VERSION:-v3.4.17}"
DIR="$(cd "$(dirname "$0")" && pwd)"
OUT="$DIR/tailwindcss"
[ -x "$OUT" ] && exit 0

case "$(uname -s)" in
  Darwin) OS=macos ;;
  Linux)  OS=linux ;;
  *) echo "Unsupported OS: $(uname -s)" >&2; exit 1 ;;
esac
case "$(uname -m)" in
  arm64|aarch64) ARCH=arm64 ;;
  x86_64|amd64)  ARCH=x64 ;;
  *) echo "Unsupported arch: $(uname -m)" >&2; exit 1 ;;
esac

URL="https://github.com/tailwindlabs/tailwindcss/releases/download/$VERSION/tailwindcss-$OS-$ARCH"
echo "Downloading $URL"
curl -fsSL "$URL" -o "$OUT.tmp"
chmod +x "$OUT.tmp"
mv "$OUT.tmp" "$OUT"
