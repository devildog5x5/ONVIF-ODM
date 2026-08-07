#!/bin/bash
set -e

#═══════════════════════════════════════════════════════════════════════════════
#
#   ╔═══════════════════════════════════════════════════════════════════════╗
#   ║                                                                       ║
#   ║            ONVIF Device Manager - CLIENT ONLY                         ║
#   ║                                                                       ║
#   ║   This installer sets up the ONVIF Device Manager CLIENT (GUI).       ║
#   ║                                                                       ║
#   ║   What you're installing:                                             ║
#   ║     ✓  Desktop application (Avalonia cross-platform GUI)              ║
#   ║     ✓  Direct ONVIF camera connection                                 ║
#   ║     ✓  Live view, PTZ control, device management                      ║
#   ║     ✓  Desktop shortcut & launcher                                    ║
#   ║                                                                       ║
#   ║   What is NOT included:                                               ║
#   ║     ✗  Background server/service                                      ║
#   ║                                                                       ║
#   ║   Use this if you want to manage cameras from this computer only.     ║
#   ║                                                                       ║
#   ╚═══════════════════════════════════════════════════════════════════════╝
#
#═══════════════════════════════════════════════════════════════════════════════

INSTALL_DIR="/opt/onvif-device-manager"
VERSION="1.0.0"

echo ""
echo "┌─────────────────────────────────────────────────────────────┐"
echo "│         ONVIF Device Manager — CLIENT Installation          │"
echo "│                      Version $VERSION                        │"
echo "├─────────────────────────────────────────────────────────────┤"
echo "│  Component: CLIENT ONLY (Desktop GUI)                       │"
echo "│  Install:   $INSTALL_DIR                         │"
echo "└─────────────────────────────────────────────────────────────┘"
echo ""

# Determine script directory
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"

# Check for .NET runtime
check_dotnet() {
    if command -v dotnet &>/dev/null; then
        echo "[✓] .NET runtime found: $(dotnet --version)"
        return 0
    fi

    echo "[!] .NET 8.0 runtime not found. Installing..."
    if [ -f /tmp/dotnet-install.sh ]; then
        rm /tmp/dotnet-install.sh
    fi
    wget -q https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
    chmod +x /tmp/dotnet-install.sh
    /tmp/dotnet-install.sh --channel 8.0 --runtime dotnet --install-dir /usr/share/dotnet
    ln -sf /usr/share/dotnet/dotnet /usr/local/bin/dotnet
    echo "[✓] .NET runtime installed"
}

echo "==> Checking prerequisites..."
check_dotnet
echo ""

echo "==> Publishing client application..."
export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"

dotnet publish "$ROOT_DIR/src/OnvifDeviceManager/OnvifDeviceManager.csproj" \
    -c Release \
    -o "/tmp/odm-client-publish" \
    --nologo -v quiet 2>&1

echo "[✓] Client built successfully"
echo ""

echo "==> Installing to $INSTALL_DIR/client..."
sudo mkdir -p "$INSTALL_DIR/client"
sudo cp -r /tmp/odm-client-publish/* "$INSTALL_DIR/client/"
sudo chmod +x "$INSTALL_DIR/client/OnvifDeviceManager"
rm -rf /tmp/odm-client-publish

echo "[✓] Client files installed"
echo ""

echo "==> Creating desktop launcher..."
sudo tee /usr/share/applications/onvif-device-manager.desktop > /dev/null <<EOF
[Desktop Entry]
Name=ONVIF Device Manager
Comment=Discover and manage ONVIF IP cameras
Exec=$INSTALL_DIR/client/OnvifDeviceManager
Icon=$INSTALL_DIR/client/onvif-icon.png
Terminal=false
Type=Application
Categories=Utility;Network;Video;
StartupWMClass=OnvifDeviceManager
EOF

sudo ln -sf "$INSTALL_DIR/client/OnvifDeviceManager" /usr/local/bin/onvif-device-manager

echo "[✓] Desktop launcher created"
echo ""

echo "┌─────────────────────────────────────────────────────────────┐"
echo "│                   Installation Complete!                     │"
echo "├─────────────────────────────────────────────────────────────┤"
echo "│                                                             │"
echo "│  Launch:    onvif-device-manager                            │"
echo "│             (or find it in your application menu)           │"
echo "│                                                             │"
echo "│  Uninstall: sudo rm -rf $INSTALL_DIR/client      │"
echo "│             sudo rm /usr/local/bin/onvif-device-manager     │"
echo "│             sudo rm /usr/share/applications/onvif-*.desktop │"
echo "│                                                             │"
echo "└─────────────────────────────────────────────────────────────┘"
echo ""
