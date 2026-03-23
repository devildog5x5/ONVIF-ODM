#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT_DIR="$(dirname "$(dirname "$SCRIPT_DIR")")"
VERSION="1.0.0"

ARCH="${1:-x64}"
PUBLISH_DIR="$ROOT_DIR/publish/OnvifDeviceManager-Avalonia-osx-${ARCH}"
APP_NAME="ONVIF Device Manager"
APP_BUNDLE="$ROOT_DIR/publish/${APP_NAME}.app"
DMG_OUTPUT="$ROOT_DIR/publish/OnvifDeviceManager-Avalonia-macOS-${ARCH}-v${VERSION}.dmg"

if [ ! -d "$PUBLISH_DIR" ]; then
    echo "Published files not found at: $PUBLISH_DIR"
    echo "Run build-all.sh first or publish manually:"
    echo "  dotnet publish src/OnvifDeviceManager -c Release -r osx-${ARCH} --self-contained -p:PublishSingleFile=true -o publish/OnvifDeviceManager-Avalonia-osx-${ARCH}"
    exit 1
fi

echo "Creating macOS app bundle for ${ARCH}..."

rm -rf "$APP_BUNDLE"
mkdir -p "$APP_BUNDLE/Contents/MacOS"
mkdir -p "$APP_BUNDLE/Contents/Resources"

cp -r "$PUBLISH_DIR"/* "$APP_BUNDLE/Contents/MacOS/"
chmod +x "$APP_BUNDLE/Contents/MacOS/OnvifDeviceManager"

# Bundle icon (CFBundleIconFile) from branding master — same artwork as warrior_icon.ico
ICON_PLIST_SNIPPET=""
MASTER="$ROOT_DIR/branding/master-icon.png"
if [ -f "$MASTER" ] && command -v sips >/dev/null 2>&1 && command -v iconutil >/dev/null 2>&1; then
    tmp=$(mktemp -d)
    iset="$tmp/AppIcon.iconset"
    mkdir -p "$iset"
    sips -z 16 16 "$MASTER" --out "$iset/icon_16x16.png" >/dev/null 2>&1
    sips -z 32 32 "$MASTER" --out "$iset/icon_16x16@2x.png" >/dev/null 2>&1
    sips -z 32 32 "$MASTER" --out "$iset/icon_32x32.png" >/dev/null 2>&1
    sips -z 64 64 "$MASTER" --out "$iset/icon_32x32@2x.png" >/dev/null 2>&1
    sips -z 128 128 "$MASTER" --out "$iset/icon_128x128.png" >/dev/null 2>&1
    sips -z 256 256 "$MASTER" --out "$iset/icon_128x128@2x.png" >/dev/null 2>&1
    sips -z 256 256 "$MASTER" --out "$iset/icon_256x256.png" >/dev/null 2>&1
    sips -z 512 512 "$MASTER" --out "$iset/icon_256x256@2x.png" >/dev/null 2>&1
    sips -z 512 512 "$MASTER" --out "$iset/icon_512x512.png" >/dev/null 2>&1
    sips -z 1024 1024 "$MASTER" --out "$iset/icon_512x512@2x.png" >/dev/null 2>&1
    if iconutil -c icns "$iset" -o "$APP_BUNDLE/Contents/Resources/AppIcon.icns" 2>/dev/null; then
        ICON_PLIST_SNIPPET='    <key>CFBundleIconFile</key>
    <string>AppIcon</string>
'
        echo "App bundle icon: AppIcon.icns (from branding/master-icon.png)"
    fi
    rm -rf "$tmp"
else
    echo "(Skipping AppIcon.icns: need macOS with sips + iconutil and branding/master-icon.png)"
fi

cat > "$APP_BUNDLE/Contents/Info.plist" << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key>
    <string>${APP_NAME}</string>
    <key>CFBundleDisplayName</key>
    <string>${APP_NAME}</string>
    <key>CFBundleIdentifier</key>
    <string>com.robertfoster.onvif-device-manager</string>
    <key>CFBundleVersion</key>
    <string>${VERSION}</string>
    <key>CFBundleShortVersionString</key>
    <string>${VERSION}</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleExecutable</key>
    <string>OnvifDeviceManager</string>
    <key>LSMinimumSystemVersion</key>
    <string>10.15</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>LSApplicationCategoryType</key>
    <string>public.app-category.utilities</string>
    <key>NSLocalNetworkUsageDescription</key>
    <string>ONVIF Device Manager needs network access to discover and manage IP cameras.</string>
${ICON_PLIST_SNIPPET}</dict>
</plist>
EOF

echo "App bundle created: $APP_BUNDLE"

if command -v hdiutil &> /dev/null; then
    echo "Creating DMG installer..."
    rm -f "$DMG_OUTPUT"

    DMG_TEMP="$ROOT_DIR/publish/dmg-temp"
    mkdir -p "$DMG_TEMP"
    cp -r "$APP_BUNDLE" "$DMG_TEMP/"
    ln -s /Applications "$DMG_TEMP/Applications"

    hdiutil create -volname "${APP_NAME}" \
        -srcfolder "$DMG_TEMP" \
        -ov -format UDZO \
        "$DMG_OUTPUT"

    rm -rf "$DMG_TEMP"
    echo "DMG created: $DMG_OUTPUT"
else
    echo "(hdiutil not available - DMG creation skipped, run on macOS)"
    echo "Compressing as tar.gz instead..."
    tar -czf "$ROOT_DIR/publish/OnvifDeviceManager-Avalonia-macOS-${ARCH}-v${VERSION}.tar.gz" \
        -C "$ROOT_DIR/publish" "${APP_NAME}.app"
fi

rm -rf "$APP_BUNDLE"
echo "Done."
