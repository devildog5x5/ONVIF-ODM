#!/bin/bash
set -e

#═══════════════════════════════════════════════════════════════════════════════
#
#   ╔═══════════════════════════════════════════════════════════════════════╗
#   ║                                                                       ║
#   ║            ONVIF Device Manager - SERVER ONLY                         ║
#   ║                                                                       ║
#   ║   This installer sets up the ONVIF Device Manager SERVER.             ║
#   ║                                                                       ║
#   ║   What you're installing:                                             ║
#   ║     ✓  REST API Server (headless, no GUI)                             ║
#   ║     ✓  ONVIF device discovery & management                            ║
#   ║     ✓  PTZ control, media profiles, network config                    ║
#   ║     ✓  Systemd service (auto-start on boot)                           ║
#   ║                                                                       ║
#   ║   What is NOT included:                                               ║
#   ║     ✗  Desktop application (GUI client)                               ║
#   ║                                                                       ║
#   ║   Use this if you want a headless server that clients connect to.     ║
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
    echo "    Save this key — clients need it to authenticate."
    echo ""
fi

echo ""
echo "┌─────────────────────────────────────────────────────────────┐"
echo "│         ONVIF Device Manager — SERVER Installation          │"
echo "│                      Version $VERSION                        │"
echo "├─────────────────────────────────────────────────────────────┤"
echo "│  Component: SERVER ONLY (REST API, no GUI)                  │"
echo "│  Bind:      $SERVER_BIND:$SERVER_PORT                                 │"
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
    /tmp/dotnet-install.sh --channel 8.0 --runtime aspnetcore --install-dir /usr/share/dotnet
    ln -sf /usr/share/dotnet/dotnet /usr/local/bin/dotnet
    echo "[✓] .NET ASP.NET Core runtime installed"
}

echo "==> Checking prerequisites..."
check_dotnet
echo ""

echo "==> Publishing server..."
export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"

dotnet publish "$ROOT_DIR/src/OnvifDeviceManager.Server/OnvifDeviceManager.Server.csproj" \
    -c Release \
    -o "/tmp/odm-server-publish" \
    --nologo -v quiet 2>&1

echo "[✓] Server built successfully"
echo ""

echo "==> Installing to $INSTALL_DIR/server..."
sudo mkdir -p "$INSTALL_DIR/server"
sudo cp -r /tmp/odm-server-publish/* "$INSTALL_DIR/server/"
sudo chmod +x "$INSTALL_DIR/server/OnvifDeviceManager.Server"
rm -rf /tmp/odm-server-publish

echo "[✓] Server files installed"
echo ""

echo "==> Creating systemd service..."
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
    echo "[✓] Systemd service created and started"
else
    echo "[!] Systemd not available — service file created but not started."
    echo "    Start manually: $INSTALL_DIR/server/OnvifDeviceManager.Server --urls http://0.0.0.0:$SERVER_PORT"
fi
echo ""

echo "┌─────────────────────────────────────────────────────────────┐"
echo "│                   Installation Complete!                     │"
echo "├─────────────────────────────────────────────────────────────┤"
echo "│                                                             │"
echo "│  Server API:  http://$SERVER_BIND:$SERVER_PORT                       │"
echo "│  API Key:     $ODM_API_KEY  │"
echo "│                                                             │"
echo "│  Status:      sudo systemctl status $SERVICE_NAME    │"
echo "│  Logs:        sudo journalctl -u $SERVICE_NAME -f    │"
echo "│  Stop:        sudo systemctl stop $SERVICE_NAME      │"
echo "│  Restart:     sudo systemctl restart $SERVICE_NAME   │"
echo "│                                                             │"
echo "│  SECURITY NOTES:                                            │"
echo "│  • API key required for all endpoints (X-API-Key header)    │"
echo "│  • Bound to $SERVER_BIND by default (local only)         │"
echo "│  • Set ODM_BIND=0.0.0.0 to expose on network               │"
echo "│  • Use HTTPS reverse proxy for production exposure          │"
echo "│                                                             │"
echo "└─────────────────────────────────────────────────────────────┘"
echo ""
