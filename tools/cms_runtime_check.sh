#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BASE_DIR="${1:-$ROOT_DIR/vendor/cms-runtime-base}"

if command -v docker-compose >/dev/null 2>&1; then
  COMPOSE=(docker-compose)
else
  COMPOSE=(docker compose)
fi

if [[ -f "$BASE_DIR/compose.yaml" ]]; then
  (cd "$BASE_DIR" && "${COMPOSE[@]}" -f compose.yaml ps)
fi

check_head() {
  local label="$1"
  local url="$2"
  echo "== $label: $url"
  curl -fsS -I "$url" | sed -n '1,8p'
}

check_head "Game client" "http://127.0.0.1:3000/"
check_head "Assets figure data" "http://127.0.0.1:8080/assets/gamedata/FigureData.json"
check_head "CMS" "http://127.0.0.1:8081/"
check_head "Imager" "http://127.0.0.1:8080/api/imager/?figure=hr-100-61.hd-180-1.ch-210-66.lg-270-82.sh-290-81"

echo "== Realtime port"
nc -vz 127.0.0.1 2096

if [[ -f "$BASE_DIR/compose.yaml" ]]; then
  echo "== Internal RCON port"
  (cd "$BASE_DIR" && "${COMPOSE[@]}" -f compose.yaml exec -T cms sh -lc 'nc -vz -w 2 arcturus 3001')
fi
