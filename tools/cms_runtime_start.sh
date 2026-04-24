#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BASE_DIR="${1:-$ROOT_DIR/vendor/cms-runtime-base}"

if [[ ! -f "$BASE_DIR/compose.yaml" ]]; then
  echo "CMS runtime base compose file not found: $BASE_DIR/compose.yaml" >&2
  echo "Run tools/cms_runtime_adopt.sh first." >&2
  exit 1
fi

echo "Hardening local CMS runtime network bindings..."
python3 - "$BASE_DIR/compose.yaml" <<'PY'
from __future__ import annotations

from pathlib import Path
import sys

path = Path(sys.argv[1])
text = path.read_text()
replacements = {
    "      - 2096:2096 # websocket port": '      - "127.0.0.1:2096:2096" # websocket port',
    "      - 3000:80": '      - "127.0.0.1:3000:80"',
    "      - 3310:3306": '      - "127.0.0.1:3310:3306"',
    "      - 8080:80": '      - "127.0.0.1:8080:80"',
    '      - "8081:8080"': '      - "127.0.0.1:8081:8080"',
}
for old, new in replacements.items():
    text = text.replace(old, new)
path.write_text(text)
PY

if command -v docker-compose >/dev/null 2>&1; then
  COMPOSE=(docker-compose)
else
  COMPOSE=(docker compose)
fi

cd "$BASE_DIR"
"${COMPOSE[@]}" -f compose.yaml up -d --build
sleep 2
"${COMPOSE[@]}" -f compose.yaml restart assets >/dev/null
"${COMPOSE[@]}" -f compose.yaml ps

env_value() {
  local key="$1"
  grep -E "^${key}=" .env | tail -n 1 | cut -d= -f2-
}

DB_USER="$(env_value MYSQL_USER)"
DB_PASSWORD="$(env_value MYSQL_PASSWORD)"
DB_NAME="$(env_value MYSQL_DATABASE)"

echo "Waiting for CMS database..."
for _ in $(seq 1 40); do
  if "${COMPOSE[@]}" -f compose.yaml exec -T db mysqladmin ping -u"$DB_USER" -p"$DB_PASSWORD" --silent >/dev/null 2>&1; then
    break
  fi
  sleep 2
done

echo "Applying local CMS runtime settings..."
"$ROOT_DIR/tools/cms_runtime_sanitize.sh" "$BASE_DIR"
