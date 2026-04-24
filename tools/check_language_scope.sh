#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
violations=0

check_scope() {
  local pattern="$1"
  local allowed_regex="$2"
  local label="$3"

  while IFS= read -r file; do
    rel="${file#$ROOT_DIR/}"
    if [[ ! "$rel" =~ $allowed_regex ]]; then
      echo "Language scope violation [$label]: $rel" >&2
      violations=$((violations + 1))
    fi
  done < <(find "$ROOT_DIR" \
    -path "$ROOT_DIR/.git" -prune -o \
    -path "$ROOT_DIR/vendor" -prune -o \
    -path "$ROOT_DIR/assets/badges/library" -prune -o \
    -type f -name "$pattern" -print)
}

check_scope "*.cs" "^(src|tests)/" "C#"
check_scope "*.vb" "^apps/epsilon-launcher-native/" "VB launcher"
check_scope "*.js" "^(apps/epsilon-launcher-desktop|src/Epsilon.Launcher/Assets)/" "JavaScript surface"
check_scope "*.py" "^tools/check_" "Python maintenance"
check_scope "*.sh" "^tools/" "Shell maintenance"
check_scope "*.sql" "^sql/" "SQL"

if (( violations > 0 )); then
  exit 1
fi

echo "Language scope check passed."
