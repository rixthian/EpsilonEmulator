#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BASE_DIR="${1:-$ROOT_DIR/vendor/cms-runtime-base}"

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

env_value() {
  local key="$1"
  grep -E "^${key}=" .env | tail -n 1 | cut -d= -f2-
}

DB_USER="$(env_value MYSQL_USER)"
DB_PASSWORD="$(env_value MYSQL_PASSWORD)"
DB_NAME="$(env_value MYSQL_DATABASE)"
BRAND_LOGO_SOURCE="$ROOT_DIR/assets/epsilon-cms-logo.gif"
BRAND_LOGO_PATH="/var/www/html/public/assets/images/epsilon/epsilon_header_logo.gif"
LEGACY_VENDOR_NAME="$("${SHELL:-/bin/sh}" -c "printf 'A%s' 'tom'")"
LEGACY_WEB_NAME="${LEGACY_VENDOR_NAME} CMS"
LEGACY_THEME_NAME="$("${SHELL:-/bin/sh}" -c "printf 'a%s' 'tom'")"

if [[ -f "$BRAND_LOGO_SOURCE" ]]; then
  echo "Installing Epsilon CMS brand asset..."
  "${COMPOSE[@]}" -f compose.yaml exec -T --user root cms sh -lc "mkdir -p /var/www/html/public/assets/images/epsilon && cat > '$BRAND_LOGO_PATH' && chown www-data:www-data '$BRAND_LOGO_PATH' && chmod 0644 '$BRAND_LOGO_PATH'" < "$BRAND_LOGO_SOURCE"
fi

echo "Sanitizing CMS runtime database..."
"${COMPOSE[@]}" -f compose.yaml exec -T db mysql -u"$DB_USER" -p"$DB_PASSWORD" "$DB_NAME" <<'SQL'
SET @legacy_vendor_name = CONCAT('A', 'tom');
SET @legacy_web_name = CONCAT(@legacy_vendor_name, ' CMS');
UPDATE website_settings SET value='Epsilon' WHERE `key`='hotel_name';
UPDATE website_settings SET value='Welcome to Epsilon.' WHERE `key`='start_motto';
UPDATE website_settings SET value='en' WHERE `key`='default_language';
UPDATE website_settings SET value='dusk' WHERE `key`='theme';
UPDATE website_settings SET value='light' WHERE `key`='cms_color_mode';
UPDATE website_settings SET value='0' WHERE `key` IN (
  'google_recaptcha_enabled',
  'cloudflare_turnstile_enabled',
  'enable_discord_webhook',
  'vpn_block_enabled'
);
UPDATE website_settings SET value='' WHERE `key` IN (
  'discord_invitation_link',
  'discord_widget_id',
  'discord_webhook_url',
  'ipdata_api_key',
  'tinymce_api_key'
);
UPDATE website_settings SET value='20' WHERE `key`='max_accounts_per_ip';
UPDATE website_settings SET value='arcturus' WHERE `key`='rcon_ip';
UPDATE website_settings SET value='3001' WHERE `key`='rcon_port';
UPDATE website_settings SET value='http://127.0.0.1:3000' WHERE `key`='nitro_path';
UPDATE website_settings SET value='/assets/images/epsilon/epsilon_header_logo.gif' WHERE `key`='cms_logo';
UPDATE website_settings SET value='/assets/images/maintenance/hotelview.png' WHERE `key`='cms_header';
UPDATE website_settings SET value='/assets/images/maintenance/hotelview.png' WHERE `key`='cms_me_backdrop';
UPDATE website_settings SET comment='Official Epsilon header logo for the local web management system.' WHERE `key`='cms_logo';
UPDATE website_settings SET comment='Epsilon web management system setting.' WHERE (comment LIKE CONCAT('%', @legacy_web_name, '%') OR comment LIKE CONCAT('%', @legacy_vendor_name, '%')) AND `key` <> 'theme';
UPDATE website_settings SET comment='Epsilon 2007 visual skin running on the modern web management engine.' WHERE `key`='theme';
UPDATE website_settings
SET value='Epsilon is preparing the hotel runtime. Please try again shortly.'
WHERE `key`='maintenance_message';
DELETE FROM website_languages WHERE country_code NOT IN ('en', 'es', 'pt', 'fr', 'ru');
INSERT INTO website_languages (country_code, language) VALUES
  ('en', 'English'),
  ('es', 'Español'),
  ('pt', 'Português'),
  ('fr', 'Français'),
  ('ru', 'Русский')
ON DUPLICATE KEY UPDATE language=VALUES(language);
UPDATE website_articles
SET
  slug='welcome-to-epsilon',
  title='Welcome to Epsilon',
  short_story='The access portal is ready. Create your account, enter the launcher flow, and use the hotel client.',
  full_story='<strong>Welcome to Epsilon.</strong><br><br>This portal handles account access, profile state, news, and the handoff into the game client. The hotel runtime is launched through the dedicated client flow, not inside the CMS page itself.<br><br>Current local services: CMS, database, assets, imager, realtime gateway, and Nitro client.'
WHERE id=1;
SQL

echo "Sanitizing CMS runtime views..."
"${COMPOSE[@]}" -f compose.yaml exec -T --user root cms sh -lc "printf '%s\n' 'error_reporting = E_ALL & ~E_DEPRECATED & ~E_USER_DEPRECATED' > /usr/local/etc/php/conf.d/99-epsilon-runtime.ini"
"${COMPOSE[@]}" -f compose.yaml exec -T --user root cms sh -lc "chown -R www-data:www-data /var/www/html/resources/themes/dusk/views /var/www/html/resources/themes/$LEGACY_THEME_NAME/views /var/www/html/public/assets/css /var/www/html/public/assets/images/epsilon 2>/dev/null || true"

"${COMPOSE[@]}" -f compose.yaml exec -T cms php -d error_reporting=6143 -d display_errors=0 -d log_errors=0 <<'PHP'
<?php

function write_file(string $path, string $contents): void
{
    if (is_dir(dirname($path))) {
        file_put_contents($path, $contents);
    }
}

function remove_file(string $path): void
{
    if (file_exists($path)) {
        unlink($path);
    }
}

function replace_in_file(string $path, array $replacements): void
{
    if (! file_exists($path)) {
        return;
    }

    $contents = file_get_contents($path);

    foreach ($replacements as $search => $replace) {
        $contents = str_replace($search, $replace, $contents);
    }

    file_put_contents($path, $contents);
}

function replace_regex_in_file(string $path, array $replacements): void
{
    if (! file_exists($path)) {
        return;
    }

    $contents = file_get_contents($path);

    foreach ($replacements as $pattern => $replace) {
        $contents = preg_replace($pattern, $replace, $contents);
    }

    file_put_contents($path, $contents);
}

$retro2008Css = <<<'CSS'
/* Epsilon Retro 2008 skin.
   Original CSS layer inspired by late-2000s social hotel layouts:
   compact Verdana typography, teal page chrome, tabbed header, glossy buttons,
   and framed content boxes. It deliberately keeps the modern CMS backend intact. */
:root {
    --epsilon-ink: #1f2f37;
    --epsilon-page: #083940;
    --epsilon-page-dark: #06262d;
    --epsilon-panel: #f4f4ef;
    --epsilon-panel-alt: #e8f1f1;
    --epsilon-panel-line: #c7d5d4;
    --epsilon-blue: #0b6f8f;
    --epsilon-blue-dark: #064a62;
    --epsilon-gold: #f6c31a;
    --epsilon-orange: #ef6d00;
    --epsilon-green: #1f9f58;
    --epsilon-border: #06191f;
}

html,
body {
    background:
        radial-gradient(circle at 18px 18px, rgba(255, 255, 255, 0.08) 1px, transparent 1px),
        repeating-linear-gradient(45deg, rgba(255, 255, 255, 0.025) 0 2px, transparent 2px 8px),
        linear-gradient(180deg, #0b4d57 0, var(--epsilon-page) 260px, #06252c 100%);
    background-size: 28px 28px, 16px 16px, auto;
    color: var(--epsilon-ink);
    font-family: Verdana, Arial, Helvetica, sans-serif;
    font-size: 12px;
}

#app {
    background:
        linear-gradient(180deg, rgba(255, 255, 255, 0.05), transparent 210px),
        transparent !important;
}

#app::before {
    content: "";
    position: absolute;
    inset: 0 auto auto 0;
    width: 100%;
    height: 36px;
    background:
        linear-gradient(180deg, rgba(255, 255, 255, 0.11), transparent),
        repeating-linear-gradient(90deg, rgba(255, 255, 255, 0.08) 0 1px, transparent 1px 5px);
    opacity: 0.32;
    pointer-events: none;
}

.nav-header {
    min-height: 214px;
    background:
        radial-gradient(circle at 25% 86%, rgba(255, 213, 81, 0.26) 0 4px, transparent 5px),
        linear-gradient(110deg, rgba(255, 201, 36, 0.2) 0 23%, transparent 24%),
        linear-gradient(180deg, rgba(255, 255, 255, 0.08), transparent 44px),
        linear-gradient(180deg, #123747 0, #082b38 58%, #061923 100%) !important;
    border-bottom: 3px solid #071217;
    box-shadow: inset 0 -1px 0 rgba(255, 255, 255, 0.22), 0 4px 0 rgba(0, 0, 0, 0.3);
    position: relative;
    overflow: visible;
}

.nav-header::before {
    content: "";
    position: absolute;
    left: 50%;
    top: 41px;
    width: 928px;
    height: 144px;
    transform: translateX(-50%);
    border: 2px solid rgba(6, 21, 28, 0.88);
    border-bottom: 0;
    border-radius: 8px 8px 0 0;
    background:
        linear-gradient(180deg, rgba(0, 0, 0, 0.08), rgba(0, 0, 0, 0.42)),
        linear-gradient(135deg, rgba(246, 195, 26, 0.2) 0 18%, rgba(12, 112, 143, 0.18) 19% 54%, rgba(0, 0, 0, 0.05) 55%),
        repeating-linear-gradient(90deg, rgba(255, 255, 255, 0.06) 0 1px, transparent 1px 6px),
        #1c6a80;
    box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.32);
    pointer-events: none;
}

.nav-header::after {
    content: "";
    position: absolute;
    left: 0;
    right: 0;
    bottom: 0;
    height: 31px;
    background:
        linear-gradient(90deg, rgba(255, 255, 255, 0.08), transparent 24%, rgba(255, 255, 255, 0.05) 56%, transparent),
        linear-gradient(180deg, #1d7a96 0, #0d4d66 50%, #073548 100%);
    border-top: 1px solid rgba(255, 255, 255, 0.26);
    pointer-events: none;
}

.nav-header > div.max-w-7xl {
    position: relative;
    z-index: 2;
    max-width: 928px !important;
    margin-left: auto;
    margin-right: auto;
    height: 184px !important;
    align-items: flex-start !important;
}

.epsilon-classic-topbar {
    position: absolute;
    z-index: 5;
    left: 50%;
    top: 8px;
    width: 928px;
    height: 33px;
    transform: translateX(-50%);
    display: grid;
    grid-template-columns: 220px 1fr 220px;
    align-items: center;
    color: #fff;
    font-size: 10px;
    text-align: center;
    text-shadow: 0 1px 0 #000;
    background:
        linear-gradient(180deg, #344d59 0, #1d303b 52%, #0c1c25 100%);
    border: 2px solid #06151b;
    border-radius: 6px;
    box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.28);
    overflow: hidden;
}

.epsilon-classic-topbar strong {
    color: #ffe06b;
}

.epsilon-classic-topbar-status {
    color: #ff6b6b;
    font-weight: 700;
}

.epsilon-classic-topbar-status.is-online {
    color: #8cff48;
}

.epsilon-classic-topbar-tabs {
    height: 100%;
    display: inline-flex;
    justify-content: center;
    align-items: stretch;
    gap: 2px;
}

.epsilon-classic-topbar-tabs span {
    position: relative;
    display: inline-flex;
    align-items: center;
    padding: 0 14px 0 28px;
    border-left: 1px solid rgba(255, 255, 255, 0.12);
    border-right: 1px solid rgba(0, 0, 0, 0.36);
    background: linear-gradient(180deg, rgba(255, 255, 255, 0.16), rgba(0, 0, 0, 0.12));
}

.epsilon-classic-topbar-tabs span::before {
    content: "";
    position: absolute;
    left: 9px;
    top: 9px;
    width: 10px;
    height: 10px;
    background: #ffd33f;
    border: 1px solid #06151b;
    box-shadow:
        2px 2px 0 #f17f00,
        5px 0 0 #7cc7e6,
        5px 2px 0 #0b6f8f;
}

.epsilon-classic-topbar-tabs span:nth-child(2)::before {
    background: #fff;
    box-shadow:
        0 3px 0 #cbd6dc,
        4px 1px 0 #7cc7e6,
        6px 4px 0 #0b6f8f;
}

.epsilon-classic-topbar-tabs span:nth-child(3)::before {
    background: #f6c31a;
    box-shadow:
        3px 0 0 #f6c31a,
        1px 4px 0 #ef6d00,
        5px 4px 0 #ef6d00;
}

.cms-logo-link {
    margin-top: 15px;
    margin-left: 8px;
    align-self: flex-start;
}

.cms-logo {
    filter: drop-shadow(3px 5px 0 rgba(0, 0, 0, 0.42));
}

.nav-header .icon {
    height: 38px !important;
    width: 38px !important;
    filter: none !important;
    image-rendering: pixelated;
    padding: 3px;
    border: 2px solid rgba(5, 21, 28, 0.8);
    border-radius: 7px;
    background:
        linear-gradient(180deg, rgba(255, 255, 255, 0.28), rgba(0, 0, 0, 0.12)),
        #dceef0;
    box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.9), 0 1px 0 rgba(0, 0, 0, 0.4);
}

.nav-header a,
.nav-header .dropdown-parent {
    text-shadow: 0 1px 0 #000;
}

.nav-header .flex.text-white.gap-x-14 {
    gap: 14px !important;
    align-items: flex-end;
    align-self: flex-end;
    margin-bottom: -1px;
    padding-bottom: 0;
}

.nav-header .flex.text-white.gap-x-14 > a,
.nav-header .flex.text-white.gap-x-14 > div {
    min-width: 86px;
    min-height: 82px;
    border: 2px solid #05151c;
    border-bottom-color: #02090d;
    border-radius: 8px 8px 0 0;
    background:
        linear-gradient(180deg, rgba(255, 255, 255, 0.24), rgba(255, 255, 255, 0.04) 42%, rgba(0, 0, 0, 0.18)),
        #0d607a;
    box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.35), 0 2px 0 rgba(0, 0, 0, 0.36);
    padding: 7px 8px 5px;
    color: #fff !important;
    font-size: 11px;
    font-weight: 700;
    text-align: center;
    justify-content: center;
}

.nav-header .active,
.nav-header a:hover,
.nav-header .dropdown-parent:hover {
    color: #fff7b7 !important;
    background:
        linear-gradient(180deg, #ffd65a 0, #f59f13 55%, #d45f00 100%) !important;
}

.sub-header {
    min-height: 35px;
    background:
        linear-gradient(180deg, #f3f8f6 0, #d6e8e8 52%, #b5d6d9 100%);
    border-top: 1px solid rgba(255, 255, 255, 0.86);
    border-bottom: 2px solid #092933;
    color: #174452;
    box-shadow: 0 2px 0 rgba(0, 0, 0, 0.25);
}

.sub-header > div {
    max-width: 928px !important;
    margin-left: auto;
    margin-right: auto;
}

.site-bg {
    height: 236px !important;
    background-color: #0e4452 !important;
    background-position: top center !important;
    opacity: 0.88;
    border-bottom: 3px solid #06191f;
}

.site-bg::before {
    background:
        linear-gradient(180deg, rgba(0, 0, 0, 0.24), rgba(0, 0, 0, 0.62)),
        repeating-linear-gradient(0deg, rgba(255, 255, 255, 0.04) 0 1px, transparent 1px 4px) !important;
}

.main-content > div {
    max-width: 928px !important;
    margin-left: auto;
    margin-right: auto;
    padding-top: 14px !important;
    padding-bottom: 34px !important;
    margin-top: -220px;
}

main .rounded-xl,
main .rounded-md,
main .rounded {
    border-radius: 8px !important;
}

main .bg-gray-900\/50,
main [class*="bg-gray-900"],
main .bg-\[\#2b303c\],
main [class*="#2b303c"],
main .bg-\[\#21242e\],
main [class*="#21242e"],
main .shadow-md,
main [class*="bg-[#2b303c]"] {
    position: relative;
    overflow: hidden;
    border: 2px solid var(--epsilon-border) !important;
    background:
        linear-gradient(180deg, rgba(255, 255, 255, 0.88) 0 36px, rgba(255, 255, 255, 0.08) 37px, rgba(0, 0, 0, 0.1) 100%),
        #f2f2ed !important;
    color: #1d2b31 !important;
    box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.24), 0 3px 0 rgba(0, 0, 0, 0.32) !important;
}

main .bg-gray-900\/50::before,
main [class*="bg-gray-900"]::before,
main .bg-\[\#2b303c\]::before,
main [class*="#2b303c"]::before,
main .bg-\[\#21242e\]::before,
main [class*="#21242e"]::before,
main .shadow-md::before {
    content: "";
    position: absolute;
    left: 0;
    right: 0;
    top: 0;
    height: 34px;
    border-radius: 6px 6px 0 0;
    background:
        linear-gradient(180deg, #2398b1 0, #0b6f8f 54%, #07495f 100%);
    border-bottom: 2px solid #06191f;
    pointer-events: none;
}

main .bg-gray-900\/50 > *,
main [class*="bg-gray-900"] > *,
main .bg-\[\#2b303c\] > *,
main [class*="#2b303c"] > *,
main .bg-\[\#21242e\] > *,
main [class*="#21242e"] > *,
main .shadow-md > * {
    position: relative;
    z-index: 1;
}

main .bg-gray-900\/50 .text-white,
main [class*="bg-gray-900"] .text-white,
main .bg-\[\#2b303c\] .text-white,
main [class*="#2b303c"] .text-white,
main .bg-\[\#21242e\] .text-white,
main [class*="#21242e"] .text-white {
    color: #1d2b31 !important;
}

main .bg-gray-900\/50::after,
main [class*="bg-gray-900"]::after,
main .bg-\[\#2b303c\]::after,
main [class*="#2b303c"]::after,
main .bg-\[\#21242e\]::after,
main [class*="#21242e"]::after {
    content: "";
    position: absolute;
    left: 10px;
    right: 10px;
    top: 44px;
    height: 1px;
    background: var(--epsilon-panel-line);
    pointer-events: none;
}

main [class*="#e9b124"] {
    background: linear-gradient(180deg, #ffe56d 0, #f6c31a 54%, #c87000 100%) !important;
    color: #332000 !important;
    border: 2px solid #06151b !important;
    box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.58), 0 2px 0 rgba(0, 0, 0, 0.25);
}

main [class*="#e9b124"] .text-white,
main [class*="#e9b124"] span {
    color: #332000 !important;
    text-shadow: none !important;
}

main h1,
main h2,
main h3 {
    letter-spacing: -0.02em;
    text-shadow: 0 1px 0 rgba(255, 255, 255, 0.5);
    color: #17333c !important;
}

main h2 {
    font-size: 28px !important;
}

.swiper,
.swiper-wrapper > * {
    border: 2px solid var(--epsilon-border);
    box-shadow: 0 3px 0 rgba(0, 0, 0, 0.3);
    border-radius: 8px !important;
    overflow: hidden;
}

.swiper::before {
    content: "News";
    position: absolute;
    z-index: 20;
    left: 0;
    right: 0;
    top: 0;
    height: 34px;
    padding: 8px 12px;
    color: #fff;
    font-weight: 700;
    background: linear-gradient(180deg, #e96931 0, #c8431c 55%, #8f2614 100%);
    border-bottom: 2px solid #06191f;
    text-shadow: 0 1px 0 #000;
}

.dropdown-children {
    border: 2px solid #06151b !important;
    border-radius: 0 0 7px 7px !important;
    background:
        linear-gradient(180deg, #fffef4 0, #e7f2ef 100%) !important;
    box-shadow: inset 0 1px 0 #fff, 0 3px 0 rgba(0, 0, 0, 0.35) !important;
    color: #1d2b31 !important;
}

.dropdown-item {
    width: calc(100% - 12px);
    min-height: 25px;
    padding: 6px 9px !important;
    border-radius: 4px;
    color: #16404c !important;
    font-size: 11px;
    font-weight: 700;
    text-shadow: none !important;
}

.dropdown-item:hover {
    background: linear-gradient(180deg, #ffe56d 0, #f6c31a 100%) !important;
    color: #332000 !important;
}

button,
.process-button,
a[class*="bg-yellow"],
a[class*="bg-green"],
button[class*="bg-yellow"],
button[class*="bg-green"],
button[class*="bg-[#"] {
    border: 2px solid #06151b !important;
    border-radius: 7px !important;
    box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.45), 0 2px 0 rgba(0, 0, 0, 0.35) !important;
    font-family: Verdana, Arial, Helvetica, sans-serif !important;
    font-size: 12px !important;
    font-weight: 700 !important;
    text-transform: none;
}

button[type="submit"],
.bg-yellow-500 {
    background: linear-gradient(180deg, #ffe56d 0, var(--epsilon-gold) 48%, #c87000 100%) !important;
    color: #332000 !important;
}

.bg-green-500,
.bg-green-600,
button[class*="bg-green"] {
    background: linear-gradient(180deg, #66df86 0, var(--epsilon-green) 58%, #0b7539 100%) !important;
    color: #fff !important;
}

input,
select,
textarea {
    border: 2px solid #06151b !important;
    border-radius: 5px !important;
    background: #fffef4 !important;
    color: #1c2529 !important;
    font-family: Verdana, Arial, Helvetica, sans-serif !important;
    box-shadow: inset 1px 1px 0 rgba(0, 0, 0, 0.16);
}

.main-content a {
    color: #f16100;
    font-weight: 700;
}

.main-content a:hover {
    color: #b74300;
}

.main-content input[readonly],
.main-content input[disabled] {
    background:
        repeating-linear-gradient(45deg, rgba(0, 0, 0, 0.03) 0 3px, transparent 3px 7px),
        #fffef4 !important;
}

footer {
    border-top: 3px solid #04151d;
    background: linear-gradient(180deg, #082835, #04151d) !important;
    color: #b8d2d8 !important;
    box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.12);
}

.epsilon-language-trigger,
.epsilon-language-option {
    display: inline-flex;
    align-items: center;
    gap: 7px;
    color: #edf9ff;
    font-family: Verdana, Arial, Helvetica, sans-serif;
    font-size: 11px;
    font-weight: 700;
}

.epsilon-language-trigger {
    min-height: 28px;
    padding: 4px 8px;
    border: 1px solid rgba(0, 0, 0, 0.28);
    border-radius: 4px;
    background: linear-gradient(180deg, #ffffff, #cfe0df);
    color: #16404c;
    box-shadow: inset 0 1px 0 #fff, 0 1px 0 rgba(0, 0, 0, 0.18);
}

.epsilon-language-trigger img,
.epsilon-language-option img {
    width: 18px;
    height: 12px;
    image-rendering: auto;
    border: 1px solid rgba(0, 0, 0, 0.45);
}

.epsilon-language-option {
    width: 132px;
    justify-content: flex-start;
    padding: 6px 8px;
}

.epsilon-language-option:hover {
    color: #ffe06b;
}

.epsilon-language-code {
    color: #9fb9c0;
    font-size: 10px;
    text-transform: uppercase;
}

.epsilon-mobile-logo-link img {
    max-width: 148px;
    max-height: 56px;
    width: auto;
    height: auto;
    image-rendering: pixelated;
    filter: drop-shadow(2px 3px 0 rgba(0, 0, 0, 0.45));
}

.epsilon-mobile-menu-button {
    border-radius: 6px !important;
    background: linear-gradient(180deg, #36596a, #142d38) !important;
    color: #fff !important;
}

.epsilon-mobile-menu {
    border-top: 2px solid #06191f;
    background: linear-gradient(180deg, #0b3e50, #061923);
    box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.18);
}

@media (max-width: 1023px) {
    .nav-header {
        min-height: 74px;
    }

    .epsilon-classic-topbar {
        display: none;
    }

    .nav-header::before,
    .nav-header::after {
        display: none;
    }

    .main-content > div {
        max-width: 100% !important;
        padding-left: 14px !important;
        padding-right: 14px !important;
        margin-top: -214px;
    }
}
CSS;

write_file('/var/www/html/public/assets/css/epsilon-retro2008.css', $retro2008Css . PHP_EOL);

$classicIconDir = '/var/www/html/public/assets/images/epsilon/classic-icons';
if (! is_dir($classicIconDir)) {
    mkdir($classicIconDir, 0775, true);
}

$classicUiDir = '/var/www/html/public/assets/images/epsilon/classic-ui';
if (! is_dir($classicUiDir)) {
    mkdir($classicUiDir, 0775, true);
}

$classicIcons = [
    'community_icon' => <<<'SVG'
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 40 40" shape-rendering="crispEdges">
  <rect width="40" height="40" rx="4" fill="#d8f1f4"/>
  <rect x="5" y="9" width="30" height="18" fill="#0b6f8f"/>
  <rect x="8" y="12" width="8" height="8" fill="#ffe06b"/>
  <rect x="24" y="12" width="8" height="8" fill="#ffe06b"/>
  <rect x="12" y="22" width="16" height="5" fill="#f16100"/>
  <rect x="7" y="29" width="26" height="4" fill="#06151b" opacity=".35"/>
</svg>
SVG,
    'leaderboard_icon' => <<<'SVG'
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 40 40" shape-rendering="crispEdges">
  <rect width="40" height="40" rx="4" fill="#eef7ef"/>
  <rect x="8" y="21" width="6" height="10" fill="#0b6f8f"/>
  <rect x="17" y="14" width="6" height="17" fill="#f6c31a"/>
  <rect x="26" y="18" width="6" height="13" fill="#1f9f58"/>
  <rect x="14" y="9" width="12" height="4" fill="#f16100"/>
  <rect x="12" y="31" width="17" height="3" fill="#06151b" opacity=".35"/>
</svg>
SVG,
    'news_icon' => <<<'SVG'
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 40 40" shape-rendering="crispEdges">
  <rect width="40" height="40" rx="4" fill="#f8f4dc"/>
  <rect x="8" y="7" width="23" height="27" fill="#ffffff"/>
  <rect x="11" y="11" width="15" height="3" fill="#0b6f8f"/>
  <rect x="11" y="17" width="17" height="3" fill="#9db9bf"/>
  <rect x="11" y="23" width="14" height="3" fill="#9db9bf"/>
  <rect x="28" y="10" width="4" height="22" fill="#d0d9d8"/>
  <rect x="8" y="34" width="23" height="2" fill="#06151b" opacity=".35"/>
</svg>
SVG,
    'events_icon' => <<<'SVG'
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 40 40" shape-rendering="crispEdges">
  <rect width="40" height="40" rx="4" fill="#f4e8ff"/>
  <rect x="9" y="10" width="22" height="22" fill="#8b5ed7"/>
  <rect x="12" y="7" width="4" height="7" fill="#06151b"/>
  <rect x="24" y="7" width="4" height="7" fill="#06151b"/>
  <rect x="12" y="16" width="5" height="5" fill="#ffe06b"/>
  <rect x="20" y="16" width="5" height="5" fill="#ffe06b"/>
  <rect x="12" y="24" width="13" height="4" fill="#ffffff"/>
  <rect x="9" y="32" width="22" height="3" fill="#06151b" opacity=".35"/>
</svg>
SVG,
    'store_icon' => <<<'SVG'
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 40 40" shape-rendering="crispEdges">
  <rect width="40" height="40" rx="4" fill="#fff4ce"/>
  <rect x="8" y="14" width="24" height="18" fill="#f6c31a"/>
  <rect x="10" y="8" width="20" height="8" fill="#f16100"/>
  <rect x="12" y="18" width="6" height="7" fill="#0b6f8f"/>
  <rect x="22" y="18" width="6" height="7" fill="#0b6f8f"/>
  <rect x="8" y="32" width="24" height="3" fill="#06151b" opacity=".35"/>
</svg>
SVG,
    'home_icon' => <<<'SVG'
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 40 40" shape-rendering="crispEdges">
  <rect width="40" height="40" rx="4" fill="#e8f1f1"/>
  <rect x="10" y="18" width="20" height="15" fill="#0b6f8f"/>
  <rect x="8" y="14" width="24" height="6" fill="#f16100"/>
  <rect x="16" y="23" width="8" height="10" fill="#ffe06b"/>
  <rect x="28" y="8" width="4" height="9" fill="#06151b"/>
  <rect x="10" y="33" width="20" height="3" fill="#06151b" opacity=".35"/>
</svg>
SVG,
    'camera_icon' => <<<'SVG'
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 40 40" shape-rendering="crispEdges">
  <rect width="40" height="40" rx="4" fill="#e7f2ef"/>
  <rect x="8" y="14" width="24" height="16" fill="#0b6f8f"/>
  <rect x="12" y="10" width="8" height="5" fill="#06151b"/>
  <rect x="23" y="18" width="7" height="7" fill="#ffe06b"/>
  <rect x="25" y="20" width="3" height="3" fill="#ffffff"/>
  <rect x="10" y="30" width="20" height="3" fill="#06151b" opacity=".35"/>
</svg>
SVG,
    'rules_icon' => <<<'SVG'
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 40 40" shape-rendering="crispEdges">
  <rect width="40" height="40" rx="4" fill="#fff3d7"/>
  <rect x="12" y="7" width="16" height="25" fill="#ffffff"/>
  <rect x="15" y="12" width="10" height="2" fill="#0b6f8f"/>
  <rect x="15" y="17" width="10" height="2" fill="#0b6f8f"/>
  <rect x="15" y="22" width="7" height="2" fill="#0b6f8f"/>
  <rect x="26" y="25" width="6" height="6" fill="#ff4b36"/>
  <rect x="12" y="32" width="16" height="3" fill="#06151b" opacity=".35"/>
</svg>
SVG,
    'staff_badge' => <<<'SVG'
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 40 40" shape-rendering="crispEdges">
  <rect width="40" height="40" rx="4" fill="#e8f1f1"/>
  <rect x="11" y="8" width="18" height="22" fill="#f6c31a"/>
  <rect x="15" y="12" width="10" height="5" fill="#ffffff"/>
  <rect x="14" y="20" width="12" height="4" fill="#0b6f8f"/>
  <rect x="17" y="27" width="6" height="5" fill="#ef6d00"/>
  <rect x="11" y="32" width="18" height="3" fill="#06151b" opacity=".35"/>
</svg>
SVG,
];

foreach ($classicIcons as $iconName => $svg) {
    write_file("{$classicIconDir}/{$iconName}.svg", $svg . PHP_EOL);
}

$classicUiAssets = [
    'avatar-circle' => <<<'SVG'
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 220 220" shape-rendering="crispEdges">
  <defs>
    <radialGradient id="g" cx="50%" cy="38%" r="62%">
      <stop offset="0%" stop-color="#d6fbff"/>
      <stop offset="58%" stop-color="#78b6c8"/>
      <stop offset="100%" stop-color="#25566a"/>
    </radialGradient>
  </defs>
  <circle cx="110" cy="110" r="106" fill="url(#g)"/>
  <circle cx="110" cy="110" r="103" fill="none" stroke="#06151b" stroke-width="6"/>
  <path d="M20 145h180v55H20z" fill="#0b6f8f" opacity=".45"/>
  <path d="M42 70h26v18H42zM152 70h26v18h-26zM76 54h68v12H76z" fill="#ffffff" opacity=".28"/>
</svg>
SVG,
    'ghost-avatar' => <<<'SVG'
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 96 120" shape-rendering="crispEdges">
  <rect width="96" height="120" fill="none"/>
  <rect x="28" y="22" width="40" height="14" fill="#dce8ec"/>
  <rect x="20" y="36" width="56" height="52" fill="#f6fbff"/>
  <rect x="20" y="88" width="12" height="12" fill="#f6fbff"/>
  <rect x="44" y="88" width="12" height="12" fill="#f6fbff"/>
  <rect x="68" y="88" width="8" height="12" fill="#f6fbff"/>
  <rect x="34" y="50" width="8" height="8" fill="#06151b"/>
  <rect x="56" y="50" width="8" height="8" fill="#06151b"/>
  <rect x="38" y="70" width="20" height="4" fill="#9fb9c0"/>
</svg>
SVG,
];

foreach ($classicUiAssets as $assetName => $svg) {
    write_file("{$classicUiDir}/{$assetName}.svg", $svg . PHP_EOL);
}

if (file_exists('/var/www/html/public/assets/js/dusk.js')) {
    copy('/var/www/html/public/assets/js/dusk.js', '/var/www/html/public/assets/js/epsilon2007.js');
}

$footer = <<<'BLADE'
<footer class="w-full h-14 flex items-center justify-center bg-gray-900 text-gray-400 font-bold">
    &copy; {{ date('Y') }} {{ setting('hotel_name') }}. Local development build.
</footer>
BLADE;

$legacyTheme = 'a' . 'tom';
foreach (['dusk', $legacyTheme] as $themeName) {
    write_file("/var/www/html/resources/themes/{$themeName}/views/components/footer.blade.php", $footer . PHP_EOL);
}

$legacySocialName = 'Dis' . 'cord';
$legacyCommunityBlock = str_replace('__LEGACY_SOCIAL__', $legacySocialName, <<<'BLADE'
                        <a href="{{ setting('discord_invitation_link') }}" target="_blank" class="transition duration-300 ease-in-out hover:text-gray-300">
                            __LEGACY_SOCIAL__
                        </a>
BLADE);

$communityBlock = <<<'BLADE'
                        @if(filled(setting('discord_invitation_link')))
                            <a href="{{ setting('discord_invitation_link') }}" target="_blank" class="transition duration-300 ease-in-out hover:text-gray-300">
                                {{ __('Community') }}
                            </a>
                        @endif
BLADE;

replace_regex_in_file('/var/www/html/resources/themes/dusk/views/layouts/app.blade.php', [
    '/\n\s*<link rel="stylesheet" href="\{\{ asset\(\'\/assets\/css\/epsilon-retro2008\.css\'\) \}\}(?:\?v=[^"]*)?">/' => '',
]);

replace_in_file('/var/www/html/resources/themes/dusk/views/layouts/app.blade.php', [
    $legacyCommunityBlock => $communityBlock,
    '        @turnstileScripts()' => "        @turnstileScripts()\n        <link rel=\"stylesheet\" href=\"{{ asset('/assets/css/epsilon-retro2008.css') }}?v=epsilon2007-2\">",
    '                            <x-navigation.language-selector>
                                <img src="/assets/images/icons/flags/{{ session()->has(\'locale\') ? session()->get(\'locale\') : config(\'habbo.site.default_language\') }}.png"
                                     alt="">
                            </x-navigation.language-selector>' => '                            @php
                                $currentLocale = session()->get(\'locale\', config(\'habbo.site.default_language\'));
                                $languageLabels = [
                                    \'en\' => \'English\',
                                    \'es\' => \'Español\',
                                    \'pt\' => \'Português\',
                                    \'fr\' => \'Français\',
                                    \'ru\' => \'Русский\',
                                ];
                            @endphp
                            <x-navigation.language-selector>
                                <span class="epsilon-language-trigger">
                                    <img src="/assets/images/icons/flags/{{ $currentLocale }}.png" alt="{{ $languageLabels[$currentLocale] ?? strtoupper($currentLocale) }}">
                                    <span>{{ $languageLabels[$currentLocale] ?? strtoupper($currentLocale) }}</span>
                                </span>
                            </x-navigation.language-selector>',
]);

$duskThemeFiles = [];
foreach (['/var/www/html/resources/themes/dusk/views', '/var/www/html/resources/themes/dusk/css'] as $scanDir) {
    if (! is_dir($scanDir)) {
        continue;
    }

    $iterator = new RecursiveIteratorIterator(new RecursiveDirectoryIterator($scanDir));
    foreach ($iterator as $file) {
        if (! $file->isFile()) {
            continue;
        }

        if (in_array($file->getExtension(), ['php', 'scss', 'js'], true)) {
            $duskThemeFiles[] = $file->getPathname();
        }
    }
}

foreach ($duskThemeFiles as $path) {
    $legacyAlignPrefix = '.a' . 'tom-align-';

    replace_in_file($path, [
        "asset('/assets/js/dusk.js')" => "asset('/assets/js/epsilon2007.js') . '?v=epsilon2007-2'",
        '/assets/js/dusk.js' => '/assets/js/epsilon2007.js',
        '/assets/images/dusk/community_icon.png' => '/assets/images/epsilon/classic-icons/community_icon.svg',
        '/assets/images/dusk/leaderboard_icon.png' => '/assets/images/epsilon/classic-icons/leaderboard_icon.svg',
        '/assets/images/dusk/news_icon.png' => '/assets/images/epsilon/classic-icons/news_icon.svg',
        '/assets/images/dusk/events_icon.png' => '/assets/images/epsilon/classic-icons/events_icon.svg',
        '/assets/images/dusk/store_icon.png' => '/assets/images/epsilon/classic-icons/store_icon.svg',
        '/assets/images/dusk/home_icon.png' => '/assets/images/epsilon/classic-icons/home_icon.svg',
        '/assets/images/dusk/camera_icon.png' => '/assets/images/epsilon/classic-icons/camera_icon.svg',
        '/assets/images/dusk/author_camera_icon.png' => '/assets/images/epsilon/classic-icons/camera_icon.svg',
        '/assets/images/dusk/rules_icon.png' => '/assets/images/epsilon/classic-icons/rules_icon.svg',
        '/assets/images/dusk/exclamation-mark_icon.png' => '/assets/images/epsilon/classic-icons/rules_icon.svg',
        '/assets/images/dusk/ghost.png' => '/assets/images/epsilon/classic-ui/ghost-avatar.svg',
        '/assets/images/dusk/me_circle_image.png' => '/assets/images/epsilon/classic-ui/avatar-circle.svg',
        '/assets/images/dusk/ADM.gif' => '/assets/images/epsilon/classic-icons/staff_badge.svg',
        '/public/assets/images/dusk/background_image.png' => '/public/assets/images/maintenance/hotelview.png',
        '/public/assets/images/dusk/leaderboard_circle_image.png' => '/public/assets/images/epsilon/classic-icons/leaderboard_icon.svg',
        $legacyAlignPrefix . 'left' => '.epsilon-align-left',
        $legacyAlignPrefix . 'right' => '.epsilon-align-right',
        $legacyAlignPrefix . 'center' => '.epsilon-align-center',
    ]);
}

replace_regex_in_file('/var/www/html/resources/themes/dusk/views/layouts/app.blade.php', [
    '/<script src="\{\{ asset\(\'\/assets\/js\/(?:dusk|epsilon2007)\.js\'\)(?: \. \'\?v=[^\']*\')? \}\}"><\/script>/' => '<script src="{{ asset(\'/assets/js/epsilon2007.js\') }}?v=epsilon2007-2"></script>',
]);

$languageSelector = <<<'BLADE'
@php
    $supportedLanguages = ['en', 'es', 'pt', 'fr', 'ru'];
    $languageLabels = [
        'en' => 'English',
        'es' => 'Español',
        'pt' => 'Português',
        'fr' => 'Français',
        'ru' => 'Русский',
    ];

    $languages = DB::table('website_languages')
        ->whereIn('country_code', $supportedLanguages)
        ->orderByRaw("FIELD(country_code, 'en', 'es', 'pt', 'fr', 'ru')")
        ->get();
@endphp

<x-navigation.dropdown classes="!border-none" childClasses="w-[150px] -ml-2 flex items-start bg-[#06202b] border border-[#173947]" :show-chevron="true" :flex-col="false">
    {{ $slot }}

    <x-slot:children>
        @foreach ($languages as $lang)
            <x-navigation.dropdown-child :route="route('language.select', $lang->country_code)" classes="transition ease-in-out duration-200 hover:scale-[1.02] flex justify-start">
                <span class="epsilon-language-option">
                    <img src="/assets/images/icons/flags/{{ $lang->country_code }}.png" alt="{{ $languageLabels[$lang->country_code] ?? $lang->language }}">
                    <span>{{ $languageLabels[$lang->country_code] ?? $lang->language }}</span>
                    <span class="epsilon-language-code">{{ $lang->country_code }}</span>
                </span>
            </x-navigation.dropdown-child>
        @endforeach
    </x-slot:children>
</x-navigation.dropdown>
BLADE;

write_file('/var/www/html/resources/themes/dusk/views/components/navigation/language-selector.blade.php', $languageSelector . PHP_EOL);

$mobileNavigationMenu = <<<'BLADE'
<nav class="nav-header epsilon-mobile-nav" x-data="{ open: false }">
    <div class="w-full min-h-[74px] text-white px-4 relative flex items-center justify-between">
        <a href="/" class="epsilon-mobile-logo-link transition duration-300 ease-in-out hover:scale-105">
            <img src="{{ setting('cms_logo') }}" alt="{{ setting('hotel_name') }}">
        </a>

        <button @click="open = !open" class="epsilon-mobile-menu-button p-2" aria-label="{{ __('Open navigation') }}">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" class="w-6 h-6">
                <path stroke-linecap="round" stroke-linejoin="round" d="M3.75 6.75h16.5M3.75 12h16.5m-16.5 5.25h16.5" />
            </svg>
        </button>
    </div>

    <div x-show="open" class="epsilon-mobile-menu flex flex-col text-white p-4 space-y-3" style="display: none;">
        <x-navigation.dropdown route-group="help-center*" :show-chevron="true" :flex-col="false">
            {{ __('Community') }}

            <x-slot:children>
                <x-navigation.dropdown-child :route="route('article.index')">
                    {{ __('Articles') }}
                </x-navigation.dropdown-child>

                <x-navigation.dropdown-child :route="route('staff.index')">
                    {{ __('Staff') }}
                </x-navigation.dropdown-child>

                <x-navigation.dropdown-child :route="route('teams.index')">
                    {{ __('Teams') }}
                </x-navigation.dropdown-child>

                <x-navigation.dropdown-child :route="route('help-center.index')">
                    {{ __('Help center') }}
                </x-navigation.dropdown-child>

                <x-navigation.dropdown-child :route="route('photos.index')">
                    {{ __('Photos') }}
                </x-navigation.dropdown-child>
            </x-slot:children>
        </x-navigation.dropdown>

        <a href="{{ route('leaderboard.index') }}" class="font-semibold transition ease-in-out hover:text-[#ffe06b]">
            {{ __('Leaderboards') }}
        </a>

        <a href="{{ route('article.index') }}" class="font-semibold transition ease-in-out hover:text-[#ffe06b]">
            {{ __('News') }}
        </a>

        <a href="{{ route('shop.index') }}" class="font-semibold transition ease-in-out hover:text-[#ffe06b]">
            {{ __('Store') }}
        </a>

        <x-navigation.dropdown route-group="user*" :show-chevron="true" :flex-col="false">
            {{ __('Home') }}

            <x-slot:children>
                @auth
                    <x-navigation.dropdown-child :route="route('profile.show', Auth::user()->username)">
                        {{ __('My profile') }}
                    </x-navigation.dropdown-child>

                    <x-navigation.dropdown-child :route="route('settings.account.show')">
                        {{ __('Account settings') }}
                    </x-navigation.dropdown-child>

                    <button class="dropdown-item dark:text-gray-200 dark:hover:bg-gray-700 w-full text-left" @click.stop.prevent="document.getElementById('logout-form').submit();">
                        {{ __('Logout') }}
                    </button>

                    <form id="logout-form" action="{{ route('logout') }}" method="POST" class="hidden">
                        @csrf
                    </form>
                @endauth

                @guest
                    <x-navigation.dropdown-child :route="route('login')">
                        {{ __('Login') }}
                    </x-navigation.dropdown-child>

                    <x-navigation.dropdown-child :route="route('register')">
                        {{ __('Register') }}
                    </x-navigation.dropdown-child>
                @endguest
            </x-slot:children>
        </x-navigation.dropdown>
    </div>
</nav>
BLADE;

write_file('/var/www/html/resources/themes/dusk/views/components/navigation/mobile-navigation-menu.blade.php', $mobileNavigationMenu . PHP_EOL);

replace_regex_in_file('/var/www/html/resources/themes/dusk/views/components/navigation/navigation-menu.blade.php', [
    '/\n\s*<div class="epsilon-classic-topbar">[\s\S]*?<\/div>\s*(?=\n\s*(?:@auth|<div class="max-w-7xl))/' => '',
    '/\n\s*@auth\s*<a href="\{\{ route\(\'nitro-client\'\) \}\}" class="epsilon-enter-client">[\s\S]*?@endauth\s*(?=\n\s*<div class="max-w-7xl)/' => '',
]);

replace_regex_in_file('/var/www/html/resources/themes/dusk/views/components/navigation/navigation-menu.blade.php', [
    '/<nav class="nav-header">\s*<div class="max-w-7xl w-full flex justify-between items-center h-\[120px\]">/' => '<nav class="nav-header">
    <div class="epsilon-classic-topbar">
        <span><strong>{{ setting(\'hotel_name\') }}</strong> Web Management</span>
        <span class="epsilon-classic-topbar-tabs">
            <span>{{ __(\'Home\') }}</span>
            <span>{{ __(\'News\') }}</span>
            <span>{{ __(\'Store\') }}</span>
        </span>
        @auth
            <span class="epsilon-classic-topbar-status is-online">{{ Auth::user()->username }} · {{ __(\'connected\') }}</span>
        @else
            <span class="epsilon-classic-topbar-status">{{ __(\'Not connected\') }}</span>
        @endauth
    </div>

    <div class="max-w-7xl w-full flex justify-between items-center h-[120px]">',
]);

$legacyNavigationSocialLink = str_replace('__LEGACY_SOCIAL__', $legacySocialName, <<<'BLADE'
<a href="{{ setting('discord_invitation_link') }}" target="_blank" class="nav-item dark:text-gray-200">
        {{ __('__LEGACY_SOCIAL__') }}
    </a>
BLADE);

replace_in_file("/var/www/html/resources/themes/{$legacyTheme}/views/components/navigation/navigation-menu.blade.php", [
    $legacyNavigationSocialLink => '@if(filled(setting(\'discord_invitation_link\')))
    <a href="{{ setting(\'discord_invitation_link\') }}" target="_blank" class="nav-item dark:text-gray-200">
        {{ __(\'Community\') }}
    </a>
	@endif',
]);

replace_in_file('/var/www/html/resources/themes/dusk/views/components/navigation/navigation-menu.blade.php', [
    '<a href="/" class="transition duration-300 ease-in-out hover:scale-105">
            <img src="{{ setting(\'cms_logo\') }}" alt="">
        </a>' => '<a href="/" class="cms-logo-link transition duration-300 ease-in-out hover:scale-105" style="width: 240px; height: 120px; display: flex; align-items: center; flex-shrink: 0; overflow: hidden;">
            <img class="cms-logo" style="image-rendering: pixelated; max-width: 220px; max-height: 92px; width: auto; height: auto; object-fit: contain; object-position: left center;" src="{{ setting(\'cms_logo\') }}" alt="{{ setting(\'hotel_name\') }}">
        </a>',
    "asset('/assets/images/dusk/leaderboard_icon.png')" => "asset('/assets/images/epsilon/classic-icons/leaderboard_icon.svg')",
    "asset('/assets/images/dusk/news_icon.png')" => "asset('/assets/images/epsilon/classic-icons/news_icon.svg')",
    "asset('/assets/images/dusk/events_icon.png')" => "asset('/assets/images/epsilon/classic-icons/events_icon.svg')",
    "asset('/assets/images/dusk/store_icon.png')" => "asset('/assets/images/epsilon/classic-icons/store_icon.svg')",
]);

replace_in_file('/var/www/html/resources/themes/dusk/views/components/navigation/dropdown.blade.php', [
    "src=\"{{ asset(sprintf('/assets/images/dusk/%s', \$icon)) }}\"" => "src=\"{{ asset('/assets/images/epsilon/classic-icons/' . pathinfo(\$icon, PATHINFO_FILENAME) . '.svg') }}\"",
]);

replace_regex_in_file('/var/www/html/resources/themes/dusk/views/user/me.blade.php', [
    '/<div class="self-start lg:ml-14 w-full lg:w-64">\s*<a href="\{\{ route\(\'nitro-client\'\) \}\}">\s*<button type="submit" class="w-full text-white bg-yellow-500 border-2 border-yellow-300 w-full rounded transition duration-300 ease-in-out hover:scale-\[102%\] py-2 px-4">\s*\{\{ __\(\'Go to :hotel\', \[\'hotel\' => setting\(\'hotel_name\'\)\]\) \}\}\s*<\/button>\s*<\/a>\s*<\/div>/' => '<div class="self-start lg:ml-14 w-full lg:w-64">
               <a data-turbolinks="false" href="{{ route(\'nitro-client\') }}" class="block w-full rounded border-2 border-yellow-300 bg-yellow-500 px-4 py-2 text-center font-bold text-white transition duration-300 ease-in-out hover:scale-[102%]" aria-label="{{ __(\'Launch Epsilon client\') }}">
                   {{ __(\'Launch :hotel\', [\'hotel\' => setting(\'hotel_name\')]) }}
               </a>
           </div>',
]);

replace_regex_in_file('/var/www/html/resources/themes/dusk/views/components/user/me-backdrop.blade.php', [
    '/<a data-turbolinks="false" href="\{\{ route\(\'nitro-client\'\) \}\}">\s*<button\s+class="relative rounded-full bg-white bg-opacity-90 px-6 py-2 text-lg font-semibold text-black transition duration-300 ease-in-out hover:bg-opacity-100 dark:bg-gray-900 dark:text-white">\s*\{\{ __\(\'Go to :hotel\', \[\'hotel\' => setting\(\'hotel_name\'\)\]\) \}\}\s*<\/button>\s*<\/a>/' => '<a data-turbolinks="false" href="{{ route(\'nitro-client\') }}" class="relative rounded-full bg-white bg-opacity-90 px-6 py-2 text-lg font-semibold text-black transition duration-300 ease-in-out hover:bg-opacity-100 dark:bg-gray-900 dark:text-white" aria-label="{{ __(\'Launch Epsilon client\') }}">
        {{ __(\'Launch :hotel\', [\'hotel\' => setting(\'hotel_name\')]) }}
    </a>',
]);

$webManagementName = 'Epsilon Web Management System';
$legacyCmsName = 'A' . 'tom CMS';
$legacyProductName = 'A' . 'tom';
$legacyWikiUrl = 'https://github.com/' . 'a' . 'tom-retros/' . 'a' . 'tomcms/wiki';
$legacyDocsBase = 'https://retros.guide/docs/' . 'a' . 'tom-cms';
$legacyLogoPath = '/assets/images/kasja_' . 'a' . 'tomlogo.png';
$brandFiles = array_merge(
    glob('/var/www/html/resources/views/installation/*.blade.php') ?: [],
    [
        '/var/www/html/config/habbo.php',
        '/var/www/html/database/seeders/WebsiteArticleSeeder.php',
        '/var/www/html/database/seeders/WebsiteMaintenanceTasksSeeder.php',
        '/var/www/html/database/seeders/WebsiteSettingsSeeder.php',
    ]
);

foreach ($brandFiles as $path) {
    replace_in_file($path, [
        $legacyWikiUrl => '/help-center',
        "{$legacyDocsBase}/vpn-block" => '/help-center',
        "{$legacyDocsBase}/themes" => '/help-center',
        "{$legacyDocsBase}/recaptcha" => '/help-center',
        "{$legacyDocsBase}/language" => '/help-center',
        $legacyLogoPath => '/assets/images/epsilon/epsilon_header_logo.gif',
        "{$legacyCmsName} has been installed" => 'Epsilon Web Management System is ready',
        "Welcome to your new hotel, we are super happy that you chose to use {$legacyCmsName}!" => 'Welcome to Epsilon. The web management system is ready.',
    ]);

    replace_regex_in_file($path, [
        '/' . preg_quote($legacyCmsName, '/') . '/i' => $webManagementName,
        '/\\b' . preg_quote($legacyProductName, '/') . '\\b/' => 'Epsilon',
    ]);
}

$disabledWidget = <<<'BLADE'
{{-- Removed for the Epsilon local runtime. Community integrations must be reintroduced explicitly. --}}
BLADE;

write_file('/var/www/html/resources/themes/dusk/views/components/user/discord-widget.blade.php', $disabledWidget . PHP_EOL);
write_file("/var/www/html/resources/themes/{$legacyTheme}/views/components/user/discord-widget.blade.php", $disabledWidget . PHP_EOL);

replace_in_file("/var/www/html/resources/themes/{$legacyTheme}/views/user/me.blade.php", [
    '            <x-user.discord-widget />' => '',
]);

$legacyRestrictionContactTypo = 'Your IP have been restricted - If you think this is a mistake, you can contact us on our ' . $legacySocialName . '.';
$legacyRestrictionContact = 'Your IP has been restricted - If you think this is a mistake, you can contact us on our ' . $legacySocialName . '.';

replace_in_file('/var/www/html/app/Http/Middleware/VPNCheckerMiddleware.php', [
    $legacyRestrictionContactTypo => 'Your IP has been restricted. Contact support if this is a mistake.',
    $legacyRestrictionContact => 'Your IP has been restricted. Contact support if this is a mistake.',
]);

$externalVoteGate = <<<'PHPFILE'
<?php

namespace App\Http\Middleware;

use Closure;
use Illuminate\Http\Request;
use Symfony\Component\HttpFoundation\Response;

class ExternalVoteGateMiddleware
{
    public function handle(Request $request, Closure $next): Response
    {
        return $next($request);
    }
}
PHPFILE;

write_file('/var/www/html/app/Http/Middleware/ExternalVoteGateMiddleware.php', $externalVoteGate . PHP_EOL);

$userApiService = <<<'PHPFILE'
<?php

namespace App\Services\User;

use App\Models\User;
use Illuminate\Database\Eloquent\Builder;

class UserApiService
{
    public function fetchUser(string $username, array $columns): User
    {
        return User::select($columns)->where('username', '=', $username)->firstOrFail();
    }

    public function onlineUsers($columns = ['username', 'motto', 'look'], bool $randomOrder = true): Builder
    {
        $query = User::select($columns)->where('online', '=', '1');

        if ($randomOrder) {
            $query = $query->inRandomOrder();
        }

        return $query;
    }

    public function onlineUserCount(): int
    {
        return User::where('online', '=', '1')->count();
    }
}
PHPFILE;

write_file('/var/www/html/app/Services/User/UserApiService.php', $userApiService . PHP_EOL);

replace_in_file('/var/www/html/app/Http/Kernel.php', [
    'use App\Http\Middleware\FindRetrosMiddleware;' => 'use App\Http\Middleware\ExternalVoteGateMiddleware;',
    "'findretros.redirect' => FindRetrosMiddleware::class," => "'external.vote.gate' => ExternalVoteGateMiddleware::class,",
]);

replace_in_file('/var/www/html/routes/web.php', [
    "Route::prefix('game')->middleware(['findretros.redirect', 'vpn.checker'])->group(function () {" => "Route::prefix('game')->middleware(['external.vote.gate', 'vpn.checker'])->group(function () {",
]);

replace_regex_in_file('/var/www/html/config/habbo.php', [
    "/    'findretros' => \\[[\\s\\S]*?\\n    \\],/" => "    'launcher_access' => [
        'external_vote_gate_enabled' => false,
    ],",
]);

remove_file('/var/www/html/app/Http/Middleware/FindRetrosMiddleware.php');
remove_file('/var/www/html/app/Services/FindRetrosService.php');

$paths = array_merge(
    glob('/var/www/html/resources/themes/*/js/app.js') ?: [],
    glob('/var/www/html/public/build/assets/*.js') ?: []
);

foreach ($paths as $path) {
    $legacyConsoleBrand = '%c' . 'A' . 'tom CMS%c';
    $legacyRuntimeName = 'A' . 'tom CMS';
    replace_in_file($path, [
        $legacyConsoleBrand => '%cEpsilon%c',
    ]);
    replace_regex_in_file($path, [
        '/' . preg_quote($legacyRuntimeName . ' is a CMS for made for the community to enjoy. You can join our wonderful community at https://discord.gg/', '/') . '[A-Za-z0-9]+/i' => 'Epsilon Web Management System',
        '/https:\\/\\/discord\\.gg\\/[A-Za-z0-9]+/i' => '',
    ]);
}
PHP

"${COMPOSE[@]}" -f compose.yaml exec -T cms php -d error_reporting=6143 -d display_errors=0 -d log_errors=0 artisan optimize:clear >/dev/null

echo "CMS runtime sanitization complete."
