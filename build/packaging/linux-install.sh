#!/bin/bash
set -e

APP_NAME="onvif-device-manager"
INSTALL_DIR="/opt/$APP_NAME"
DESKTOP_FILE="/usr/share/applications/$APP_NAME.desktop"
BIN_LINK="/usr/local/bin/$APP_NAME"

if [ "$EUID" -ne 0 ]; then
    echo "Please run as root (sudo ./linux-install.sh)"
    exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

echo "Installing ONVIF Device Manager..."

mkdir -p "$INSTALL_DIR"
cp -r "$SCRIPT_DIR"/* "$INSTALL_DIR/"
rm -f "$INSTALL_DIR/linux-install.sh" "$INSTALL_DIR/linux-uninstall.sh"

chmod +x "$INSTALL_DIR/OnvifDeviceManager"

cat > "$DESKTOP_FILE" << 'EOF'
[Desktop Entry]
Name=ONVIF Device Manager
Comment=Discover and manage ONVIF IP cameras
Exec=/opt/onvif-device-manager/OnvifDeviceManager
Terminal=false
Type=Application
Categories=Utility;Network;Video;
Keywords=ONVIF;Camera;IP;PTZ;Surveillance;
StartupWMClass=OnvifDeviceManager
EOF

ln -sf "$INSTALL_DIR/OnvifDeviceManager" "$BIN_LINK"

echo ""
echo "Installation complete!"
echo "  Installed to: $INSTALL_DIR"
echo "  Desktop entry: $DESKTOP_FILE"
echo "  Command: $APP_NAME"
echo ""
echo "Run with: $APP_NAME"
