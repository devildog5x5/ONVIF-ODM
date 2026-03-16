#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
OUTPUT_DIR="$ROOT_DIR/publish"
VERSION="1.0.0"

echo "============================================"
echo " ONVIF Device Manager - Build All Platforms"
echo " Version: $VERSION"
echo "============================================"
echo ""

rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"

publish() {
    local PROJECT=$1
    local RID=$2
    local NAME=$3
    local OUT="$OUTPUT_DIR/$NAME"

    echo ">> Publishing $NAME ($RID)..."
    dotnet publish "$ROOT_DIR/src/$PROJECT" \
        -c Release \
        -r "$RID" \
        --self-contained true \
        -p:PublishSingleFile=true \
        -p:PublishTrimmed=false \
        -p:IncludeNativeLibrariesForSelfExtract=true \
        -p:Version="$VERSION" \
        -o "$OUT" \
        --nologo -v quiet

    echo "   Done: $OUT"
}

echo "[1/5] Building WPF (Windows x64)..."
publish "OnvifDeviceManager.Wpf/OnvifDeviceManager.Wpf.csproj" "win-x64" "OnvifDeviceManager-Wpf-win-x64"

echo "[2/5] Building Avalonia (Windows x64)..."
publish "OnvifDeviceManager/OnvifDeviceManager.csproj" "win-x64" "OnvifDeviceManager-Avalonia-win-x64"

echo "[3/5] Building Avalonia (Linux x64)..."
publish "OnvifDeviceManager/OnvifDeviceManager.csproj" "linux-x64" "OnvifDeviceManager-Avalonia-linux-x64"

echo "[4/5] Building Avalonia (macOS x64)..."
publish "OnvifDeviceManager/OnvifDeviceManager.csproj" "osx-x64" "OnvifDeviceManager-Avalonia-osx-x64"

echo "[5/5] Building Avalonia (macOS ARM64)..."
publish "OnvifDeviceManager/OnvifDeviceManager.csproj" "osx-arm64" "OnvifDeviceManager-Avalonia-osx-arm64"

echo ""
echo "============================================"
echo " Creating archives..."
echo "============================================"

cd "$OUTPUT_DIR"

for dir in */; do
    dir_name="${dir%/}"
    echo ">> Archiving $dir_name..."
    if [[ "$dir_name" == *"win"* ]]; then
        zip -qr "${dir_name}-v${VERSION}.zip" "$dir_name/"
    else
        tar -czf "${dir_name}-v${VERSION}.tar.gz" "$dir_name/"
    fi
done

echo ""
echo "============================================"
echo " Build complete! Output:"
echo "============================================"
ls -lh "$OUTPUT_DIR"/*.{zip,tar.gz} 2>/dev/null || ls -lh "$OUTPUT_DIR"/
echo ""
echo "Done."
