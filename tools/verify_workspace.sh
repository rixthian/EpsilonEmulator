#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

python3 "$ROOT_DIR/tools/check_repo_hygiene.py"
python3 "$ROOT_DIR/tools/check_file_naming.py"
"$ROOT_DIR/tools/check_language_scope.sh"
git -C "$ROOT_DIR" diff --check

dotnet test "$ROOT_DIR/EpsilonEmulator.sln"
dotnet build "$ROOT_DIR/apps/epsilon-launcher-native/EpsilonLauncher.sln"

if [[ "${SKIP_NODE_CHECK:-0}" != "1" ]]; then
  (cd "$ROOT_DIR/apps/epsilon-launcher-desktop" && npm install && npm run check)
fi

if [[ "${SKIP_RUNTIME_CHECK:-0}" != "1" && -f "$ROOT_DIR/vendor/cms-runtime-base/compose.yaml" ]]; then
  "$ROOT_DIR/tools/cms_runtime_check.sh"
fi

"$ROOT_DIR/tools/clean_workspace.sh"

echo "Workspace verification passed."
