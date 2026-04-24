#!/bin/bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_FILE="$ROOT_DIR/HabboCustomLauncher.vbproj"
PUBLISH_DIR="$ROOT_DIR/bin/Release/net8.0/osx-arm64/publish"
DIST_DIR="$ROOT_DIR/dist/macos"
APP_DIR="$DIST_DIR/EpsilonLauncher.app"
ICONSET_DIR="$DIST_DIR/EpsilonLauncher.iconset"
STAGING_DIR="$DIST_DIR/staging"
DMG_PATH="$ROOT_DIR/dist/EpsilonLauncher-macOS-arm64.dmg"
ICON_SOURCE="$ROOT_DIR/Assets/HabboCustomLauncherIcon.png"
VERSION="$(python3 - <<'PY'
from pathlib import Path
import re
text = Path('/Users/yasminluengo/Documents/Playground/EpsilonEmulator/Directory.Build.props').read_text()
match = re.search(r'<Version>([^<]+)</Version>', text)
print(match.group(1) if match else '0.0.0')
PY
)"

rm -rf "$DIST_DIR" "$DMG_PATH"
mkdir -p "$DIST_DIR"

dotnet publish "$PROJECT_FILE" -c Release -r osx-arm64 --self-contained true

mkdir -p "$APP_DIR/Contents/MacOS" "$APP_DIR/Contents/Resources"
cp -R "$PUBLISH_DIR/"* "$APP_DIR/Contents/MacOS/"

mkdir -p "$ICONSET_DIR"
sips -z 16 16 "$ICON_SOURCE" --out "$ICONSET_DIR/icon_16x16.png" >/dev/null
sips -z 32 32 "$ICON_SOURCE" --out "$ICONSET_DIR/icon_16x16@2x.png" >/dev/null
sips -z 32 32 "$ICON_SOURCE" --out "$ICONSET_DIR/icon_32x32.png" >/dev/null
sips -z 64 64 "$ICON_SOURCE" --out "$ICONSET_DIR/icon_32x32@2x.png" >/dev/null
sips -z 128 128 "$ICON_SOURCE" --out "$ICONSET_DIR/icon_128x128.png" >/dev/null
sips -z 256 256 "$ICON_SOURCE" --out "$ICONSET_DIR/icon_128x128@2x.png" >/dev/null
sips -z 256 256 "$ICON_SOURCE" --out "$ICONSET_DIR/icon_256x256.png" >/dev/null
sips -z 512 512 "$ICON_SOURCE" --out "$ICONSET_DIR/icon_256x256@2x.png" >/dev/null
sips -z 512 512 "$ICON_SOURCE" --out "$ICONSET_DIR/icon_512x512.png" >/dev/null
sips -z 1024 1024 "$ICON_SOURCE" --out "$ICONSET_DIR/icon_512x512@2x.png" >/dev/null
iconutil -c icns "$ICONSET_DIR" -o "$APP_DIR/Contents/Resources/EpsilonLauncher.icns"

cat > "$APP_DIR/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleDevelopmentRegion</key>
  <string>en</string>
  <key>CFBundleExecutable</key>
  <string>EpsilonLauncher</string>
  <key>CFBundleIconFile</key>
  <string>EpsilonLauncher.icns</string>
  <key>CFBundleIdentifier</key>
  <string>com.epsilon.hotel.launcher</string>
  <key>CFBundleInfoDictionaryVersion</key>
  <string>6.0</string>
  <key>CFBundleName</key>
  <string>EpsilonLauncher</string>
  <key>CFBundlePackageType</key>
  <string>APPL</string>
  <key>CFBundleShortVersionString</key>
  <string>${VERSION}</string>
  <key>CFBundleURLTypes</key>
  <array>
    <dict>
      <key>CFBundleTypeRole</key>
      <string>Viewer</string>
      <key>CFBundleURLName</key>
      <string>com.epsilon.hotel.launcher</string>
      <key>CFBundleURLSchemes</key>
      <array>
        <string>epsilonlauncher</string>
      </array>
    </dict>
  </array>
  <key>CFBundleVersion</key>
  <string>${VERSION}</string>
  <key>LSMinimumSystemVersion</key>
  <string>12.0</string>
  <key>NSHighResolutionCapable</key>
  <true/>
</dict>
</plist>
PLIST

codesign --force --deep --sign - "$APP_DIR"

mkdir -p "$STAGING_DIR"
cp -R "$APP_DIR" "$STAGING_DIR/"
ln -s /Applications "$STAGING_DIR/Applications"

hdiutil create -volname "Epsilon Launcher" -srcfolder "$STAGING_DIR" -ov -format UDZO "$DMG_PATH" >/dev/null

echo "DMG created at: $DMG_PATH"
