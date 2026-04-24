#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BASE_DIR="${1:-$ROOT_DIR/vendor/cms-runtime-base}"
BASE_URL="${EPSILON_CMS_URL:-http://127.0.0.1:8081}"

if command -v docker-compose >/dev/null 2>&1; then
  COMPOSE=(docker-compose)
else
  COMPOSE=(docker compose)
fi

if [[ ! -f "$BASE_DIR/compose.yaml" ]]; then
  echo "CMS runtime base compose file not found: $BASE_DIR/compose.yaml" >&2
  exit 1
fi

cd "$BASE_DIR"

tmp_dir="$(mktemp -d)"
trap 'rm -rf "$tmp_dir"' EXIT

cookie="$tmp_dir/cookie.txt"
register_html="$tmp_dir/register.html"
headers="$tmp_dir/register.headers"
me_html="$tmp_dir/me.html"
nitro_html="$tmp_dir/nitro.html"

status() {
  curl -sS -o "$tmp_dir/body" -w '%{http_code}' "$1"
}

echo "== Containers"
"${COMPOSE[@]}" -f compose.yaml ps

echo "== Internal ports"
"${COMPOSE[@]}" -f compose.yaml exec -T cms sh -lc 'nc -vz -w 2 arcturus 3001 && nc -vz -w 2 arcturus 2096'

echo "== Register page"
curl -sS -c "$cookie" "$BASE_URL/register" -o "$register_html"
if ! grep -q 'name="_token"' "$register_html"; then
  echo "Registration CSRF token not found." >&2
  exit 1
fi
if grep -q 'cf-turnstile' "$register_html"; then
  echo "Cloudflare Turnstile widget is still rendered; disable it for local registration." >&2
  exit 1
fi

token="$(perl -0777 -ne 'print $1 if /name="_token" value="([^"]+)"/s' "$register_html")"
user="cmsaudit$(date +%s)"
email="${user}@example.test"

"${COMPOSE[@]}" -f compose.yaml exec -T cms sh -lc ': > /var/www/html/storage/logs/laravel.log'

post_status="$(curl -sS -i -c "$cookie" -b "$cookie" \
  -H 'Content-Type: application/x-www-form-urlencoded' \
  -H "Referer: $BASE_URL/register" \
  --data-urlencode "_token=$token" \
  --data-urlencode "username=$user" \
  --data-urlencode "mail=$email" \
  --data-urlencode 'password=TestPassword123!' \
  --data-urlencode 'password_confirmation=TestPassword123!' \
  --data-urlencode 'terms=on' \
  --data-urlencode 'referral_code=' \
  "$BASE_URL/register" -o "$headers" -w '%{http_code}')"

if [[ "$post_status" != "302" ]] || ! grep -qi '^Location: .*/user/me' "$headers"; then
  echo "Registration did not redirect to /user/me. HTTP $post_status" >&2
  sed -n '1,40p' "$headers" >&2
  exit 1
fi

me_status="$(curl -sS -b "$cookie" "$BASE_URL/user/me" -o "$me_html" -w '%{http_code}')"
if [[ "$me_status" != "200" ]]; then
  echo "Authenticated /user/me check failed. HTTP $me_status" >&2
  exit 1
fi

nitro_status="$(curl -sS -b "$cookie" "$BASE_URL/game/nitro" -o "$nitro_html" -w '%{http_code}')"
if [[ "$nitro_status" != "200" ]] || ! grep -q 'http://127.0.0.1:3000/index.html?sso=' "$nitro_html"; then
  echo "Nitro handoff is not pointing at the local client." >&2
  exit 1
fi

if "${COMPOSE[@]}" -f compose.yaml exec -T cms sh -lc 'test -s /var/www/html/storage/logs/laravel.log'; then
  echo "Laravel logged errors during audit:" >&2
  "${COMPOSE[@]}" -f compose.yaml exec -T cms sh -lc 'cat /var/www/html/storage/logs/laravel.log' >&2
  exit 1
fi

DB_USER="$(grep -E '^MYSQL_USER=' .env | tail -n 1 | cut -d= -f2-)"
DB_PASSWORD="$(grep -E '^MYSQL_PASSWORD=' .env | tail -n 1 | cut -d= -f2-)"
DB_NAME="$(grep -E '^MYSQL_DATABASE=' .env | tail -n 1 | cut -d= -f2-)"
"${COMPOSE[@]}" -f compose.yaml exec -T db mysql -u"$DB_USER" -p"$DB_PASSWORD" "$DB_NAME" \
  -e "DELETE FROM users WHERE username='${user}' AND mail='${email}'"

echo "CMS audit passed."
echo "Temporary test user created and removed: $user"
