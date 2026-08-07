#!/bin/bash
set -e

echo ""
echo "┌─────────────────────────────────────────────────────────────┐"
echo "│       ONVIF Device Manager — Uninstall                      │"
echo "└─────────────────────────────────────────────────────────────┘"
echo ""

SERVICE_NAME="onvif-device-manager"
INSTALL_DIR="/opt/onvif-device-manager"

if systemctl is-active --quiet $SERVICE_NAME 2>/dev/null; then
    echo "==> Stopping server service..."
    sudo systemctl stop $SERVICE_NAME
    sudo systemctl disable $SERVICE_NAME
    sudo rm -f /etc/systemd/system/$SERVICE_NAME.service
    sudo systemctl daemon-reload
    echo "[✓] Service removed"
fi

if [ -d "$INSTALL_DIR" ]; then
    echo "==> Removing installed files..."
    sudo rm -rf "$INSTALL_DIR"
    echo "[✓] Files removed from $INSTALL_DIR"
fi

if [ -f /usr/local/bin/onvif-device-manager ]; then
    sudo rm -f /usr/local/bin/onvif-device-manager
    echo "[✓] CLI symlink removed"
fi

if [ -f /usr/share/applications/onvif-device-manager.desktop ]; then
    sudo rm -f /usr/share/applications/onvif-device-manager.desktop
    echo "[✓] Desktop entry removed"
fi

echo ""
echo "[✓] Uninstall complete."
echo ""
