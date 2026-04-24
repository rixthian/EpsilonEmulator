#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SOURCE_DIR="${1:-${EPSILON_VENDOR_BASE_SOURCE:-}}"
TARGET_DIR="${2:-$ROOT_DIR/vendor/cms-runtime-base}"
DB_USER="${EPSILON_VENDOR_BASE_DB_USER:-}"
DB_PASSWORD="${EPSILON_VENDOR_BASE_DB_PASSWORD:-}"
DB_NAME="${EPSILON_VENDOR_BASE_DB_NAME:-}"

if [[ -z "$SOURCE_DIR" || ! -d "$SOURCE_DIR" ]]; then
  echo "Source CMS runtime base directory not found. Pass it as the first argument or set EPSILON_VENDOR_BASE_SOURCE." >&2
  exit 1
fi

if [[ -z "$DB_USER" || -z "$DB_PASSWORD" || -z "$DB_NAME" ]]; then
  echo "Set EPSILON_VENDOR_BASE_DB_USER, EPSILON_VENDOR_BASE_DB_PASSWORD, and EPSILON_VENDOR_BASE_DB_NAME before exporting DB." >&2
  exit 1
fi

if command -v docker-compose >/dev/null 2>&1; then
  COMPOSE=(docker-compose)
else
  COMPOSE=(docker compose)
fi

mkdir -p "$TARGET_DIR"

echo "Copying CMS/assets/runtime base into: $TARGET_DIR"
rsync -a --delete \
  --exclude '.git/' \
  --exclude 'db/data/' \
  --exclude 'db/backup/' \
  --exclude 'atomcms/logs/' \
  --exclude 'atomcms/storage/framework/cache/' \
  --exclude 'atomcms/storage/framework/sessions/' \
  --exclude 'atomcms/storage/framework/views/' \
  "$SOURCE_DIR"/ "$TARGET_DIR"/

mkdir -p "$TARGET_DIR/db/dumps"

echo "Exporting MySQL CMS database as portable bootstrap dump..."
(
  cd "$SOURCE_DIR"
  "${COMPOSE[@]}" -f compose.yaml exec -T db \
    mysqldump \
      -u"$DB_USER" \
      -p"$DB_PASSWORD" \
      --single-transaction \
      --routines \
      --triggers \
      --no-tablespaces \
      --default-character-set=utf8mb4 \
      "$DB_NAME" \
    | gzip -c > "$TARGET_DIR/db/dumps/epsilon_cms_runtime.sql.gz"
)

cat > "$TARGET_DIR/ADOPTED_FROM.txt" <<EOF
Adopted by Epsilon local base tooling.

Source: $SOURCE_DIR
Target: $TARGET_DIR
Date UTC: $(date -u +"%Y-%m-%dT%H:%M:%SZ")

Policy:
- This directory is local runtime state and is intentionally gitignored.
- The upstream stack is AGPL-3.0; do not merge source into Epsilon without a license decision.
- Assets must be treated as provenance-sensitive.
- DB is stored as a portable SQL dump, not a live MySQL data directory.
EOF

echo "Adoption complete."
echo "Target: $TARGET_DIR"
echo "DB dump: $TARGET_DIR/db/dumps/epsilon_cms_runtime.sql.gz"
