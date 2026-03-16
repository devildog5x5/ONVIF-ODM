#!/bin/bash
set -e

APP_NAME="onvif-device-manager"
INSTALL_DIR="/opt/$APP_NAME"
DESKTOP_FILE="/usr/share/applications/$APP_NAME.desktop"
BIN_LINK="/usr/local/bin/$APP_NAME"

if [ "$EUID" -ne 0 ]; then
    echo "Please run as root (sudo ./linux-uninstall.sh)"
    exit 1
fi

echo "Uninstalling ONVIF Device Manager..."

rm -rf "$INSTALL_DIR"
rm -f "$DESKTOP_FILE"
rm -f "$BIN_LINK"

echo "Uninstall complete."
