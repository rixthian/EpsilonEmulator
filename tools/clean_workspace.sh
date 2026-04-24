#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

rm -rf \
  "$ROOT_DIR/apps/epsilon-launcher-desktop/node_modules" \
  "$ROOT_DIR/apps/epsilon-launcher-native/bin" \
  "$ROOT_DIR/apps/epsilon-launcher-native/obj"

find "$ROOT_DIR/src" "$ROOT_DIR/tests" \
  -type d \( -name bin -o -name obj \) \
  -prune -exec rm -rf {} +

find "$ROOT_DIR/apps" "$ROOT_DIR/src" "$ROOT_DIR/tests" "$ROOT_DIR/tools" \
  -type d \( -name __pycache__ -o -name node_modules \) \
  -prune -exec rm -rf {} +

find "$ROOT_DIR" \
  -path "$ROOT_DIR/.git" -prune -o \
  -path "$ROOT_DIR/vendor" -prune -o \
  -path "$ROOT_DIR/assets/badges/library" -prune -o \
  -type f \( -name ".DS_Store" -o -name "Thumbs.db" -o -name "*.tmp" -o -name "*.log" -o -name "*.pyc" -o -name "*.pyo" \) \
  -delete

echo "Workspace cleaned."
