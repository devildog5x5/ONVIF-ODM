#!/bin/bash
set -e

#═══════════════════════════════════════════════════════════════════════════════
#
#   ╔═══════════════════════════════════════════════════════════════════════╗
#   ║                                                                       ║
#   ║            ONVIF Device Manager - FULL INSTALLATION                   ║
#   ║              (Server + Client — Everything Included)                   ║
#   ║                                                                       ║
#   ║   This installer sets up BOTH the server AND client together.         ║
#   ║                                                                       ║
#   ║   What you're installing:                                             ║
#   ║     ✓  REST API Server (runs as background service)                   ║
#   ║     ✓  Desktop application (Avalonia cross-platform GUI)              ║
#   ║     ✓  ONVIF device discovery & management                            ║
#   ║     ✓  Live view, PTZ control, media profiles                         ║
#   ║     ✓  Systemd service (server auto-starts on boot)                   ║
#   ║     ✓  Desktop shortcut & launcher                                    ║
#   ║                                                                       ║
#   ║   Use this for a complete all-in-one setup on a single machine.       ║
#   ║                                                                       ║
#   ╚═══════════════════════════════════════════════════════════════════════╝
#
#═══════════════════════════════════════════════════════════════════════════════

INSTALL_DIR="/opt/onvif-device-manager"
SERVICE_NAME="onvif-device-manager"
SERVER_PORT="${ODM_PORT:-5000}"
SERVER_BIND="${ODM_BIND:-127.0.0.1}"
VERSION="1.0.0"

# Generate a secure API key if not provided
if [ -z "$ODM_API_KEY" ]; then
    ODM_API_KEY=$(openssl rand -base64 32 | tr -d '/+=' | head -c 32)
    echo "[!] Generated API key: $ODM_API_KEY"
    echo "    Save this key — clients need it to authenticate with the server."
    echo ""
fi

echo ""
echo "┌─────────────────────────────────────────────────────────────┐"
echo "│       ONVIF Device Manager — FULL Installation              │"
echo "│                      Version $VERSION                        │"
echo "├─────────────────────────────────────────────────────────────┤"
echo "│  Components: SERVER + CLIENT (complete package)             │"
echo "│  Server:     REST API on port $SERVER_PORT                       │"
echo "│  Client:     Desktop GUI application                        │"
echo "│  Install:    $INSTALL_DIR                        │"
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
    /tmp/dotnet-install.sh --channel 8.0 --install-dir /usr/share/dotnet
    ln -sf /usr/share/dotnet/dotnet /usr/local/bin/dotnet
    echo "[✓] .NET runtime installed (SDK)"
}

echo "==> Checking prerequisites..."
check_dotnet
echo ""

# ─────────────────────────────────────────────────────────────────────────────
# SERVER
# ─────────────────────────────────────────────────────────────────────────────
echo "═══════════════════════════════════════════════════════════════"
echo "  STEP 1 of 2: Installing SERVER"
echo "═══════════════════════════════════════════════════════════════"
echo ""

echo "==> Publishing server..."
export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"

dotnet publish "$ROOT_DIR/src/OnvifDeviceManager.Server/OnvifDeviceManager.Server.csproj" \
    -c Release \
    -o "/tmp/odm-server-publish" \
    --nologo -v quiet 2>&1

echo "[✓] Server built"

sudo mkdir -p "$INSTALL_DIR/server"
sudo cp -r /tmp/odm-server-publish/* "$INSTALL_DIR/server/"
sudo chmod +x "$INSTALL_DIR/server/OnvifDeviceManager.Server"
rm -rf /tmp/odm-server-publish

echo "[✓] Server files installed"

sudo tee /etc/systemd/system/$SERVICE_NAME.service > /dev/null <<EOF
[Unit]
Description=ONVIF Device Manager Server
After=network.target

[Service]
Type=simple
ExecStart=$INSTALL_DIR/server/OnvifDeviceManager.Server --urls http://$SERVER_BIND:$SERVER_PORT
WorkingDirectory=$INSTALL_DIR/server
Restart=always
RestartSec=5
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_ROOT=$DOTNET_ROOT
Environment=ODM_API_KEY=$ODM_API_KEY

[Install]
WantedBy=multi-user.target
EOF

if pidof systemd &>/dev/null || [ "$(cat /proc/1/comm 2>/dev/null)" = "systemd" ]; then
    sudo systemctl daemon-reload
    sudo systemctl enable $SERVICE_NAME
    sudo systemctl start $SERVICE_NAME
    echo "[✓] Server service enabled and started on port $SERVER_PORT"
else
    echo "[!] Systemd not available — service file created but not auto-started."
    echo "    Start manually: $INSTALL_DIR/server/OnvifDeviceManager.Server --urls http://0.0.0.0:$SERVER_PORT"
fi
echo ""

# ─────────────────────────────────────────────────────────────────────────────
# CLIENT
# ─────────────────────────────────────────────────────────────────────────────
echo "═══════════════════════════════════════════════════════════════"
echo "  STEP 2 of 2: Installing CLIENT"
echo "═══════════════════════════════════════════════════════════════"
echo ""

echo "==> Publishing client..."
dotnet publish "$ROOT_DIR/src/OnvifDeviceManager/OnvifDeviceManager.csproj" \
    -c Release \
    -o "/tmp/odm-client-publish" \
    --nologo -v quiet 2>&1

echo "[✓] Client built"

sudo mkdir -p "$INSTALL_DIR/client"
sudo cp -r /tmp/odm-client-publish/* "$INSTALL_DIR/client/"
sudo chmod +x "$INSTALL_DIR/client/OnvifDeviceManager"
rm -rf /tmp/odm-client-publish

echo "[✓] Client files installed"

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
echo "│              Full Installation Complete!                     │"
echo "├─────────────────────────────────────────────────────────────┤"
echo "│                                                             │"
echo "│  ┌── SERVER ──────────────────────────────────────────────┐ │"
echo "│  │  API:      http://localhost:$SERVER_PORT                     │ │"
echo "│  │  Status:   sudo systemctl status $SERVICE_NAME  │ │"
echo "│  │  Logs:     sudo journalctl -u $SERVICE_NAME -f  │ │"
echo "│  └────────────────────────────────────────────────────────┘ │"
echo "│                                                             │"
echo "│  ┌── CLIENT ──────────────────────────────────────────────┐ │"
echo "│  │  Launch:   onvif-device-manager                        │ │"
echo "│  │            (or find in application menu)               │ │"
echo "│  └────────────────────────────────────────────────────────┘ │"
echo "│                                                             │"
echo "│  Uninstall: sudo ./install/uninstall.sh                     │"
echo "│                                                             │"
echo "└─────────────────────────────────────────────────────────────┘"
echo ""
