#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

echo "== Top-level weight"
du -hd 1 "$ROOT_DIR" | sort -h

echo
echo "== Heaviest project files, excluding git/vendor/badge cache"
find "$ROOT_DIR" \
  -path "$ROOT_DIR/.git" -prune -o \
  -path "$ROOT_DIR/vendor" -prune -o \
  -path "$ROOT_DIR/assets/badges/library" -prune -o \
  -type f -exec du -h {} + | sort -h | tail -40
