#!/bin/bash
set -e

# Must run as root
if [ "$(id -u)" -ne 0 ]; then
  echo "This script must be run as root" >&2
  exit 1
fi

# Determine installation location
INSTALL_DIR="/opt/conductor-health-check"
SERVICE_NAME="conductor-health-check"
EXECUTABLE="Conductor_Health_Check"
CURRENT_DIR=$(pwd)

echo "Installing/Updating Conductor Health Check as a systemd service..."

# Check if service exists and is running
SERVICE_EXISTS=false
SERVICE_RUNNING=false

if systemctl list-units --full -all | grep -Fq "$SERVICE_NAME.service"; then
    SERVICE_EXISTS=true
    echo "Service already exists, preparing for update..."
    
    if systemctl is-active --quiet "$SERVICE_NAME"; then
        SERVICE_RUNNING=true
        echo "Service is currently running, stopping it for update..."
        systemctl stop "$SERVICE_NAME"
    fi
fi

# Create installation directory
mkdir -p $INSTALL_DIR

# Backup existing appsettings.json if it exists during an update
if [ "$SERVICE_EXISTS" = true ] && [ -f "$INSTALL_DIR/appsettings.json" ]; then
    echo "Backing up existing appsettings.json..."
    cp "$INSTALL_DIR/appsettings.json" "$INSTALL_DIR/appsettings.json.backup"
fi

# Copy all files to installation directory
# Use rsync if available for better handling of file updates, otherwise use cp
if command -v rsync >/dev/null 2>&1; then
    echo "Using rsync for file copy..."
    rsync -av --exclude='install-service.sh' "$CURRENT_DIR/" "$INSTALL_DIR/"
else
    echo "Using cp for file copy..."
    # Remove the install script from destination to avoid copying it
    find "$CURRENT_DIR" -mindepth 1 -maxdepth 1 ! -name 'install-service.sh' -exec cp -r {} "$INSTALL_DIR/" \;
fi

# Handle appsettings.json configuration
if [ "$SERVICE_EXISTS" = true ] && [ -f "$INSTALL_DIR/appsettings.json.backup" ]; then
    # Check if user modified appsettings.json in the extracted folder
    if ! cmp -s "$CURRENT_DIR/appsettings.json" "$INSTALL_DIR/appsettings.json"; then
        echo "WARNING: You have modified appsettings.json in the extracted folder."
        echo "Current service configuration will be preserved, but your extracted changes will be ignored."
        echo "If you want to use the modified configuration, please:"
        echo "  1. Stop this installation (Ctrl+C)"
        echo "  2. Manually merge your changes into the existing service config at:"
        echo "     $INSTALL_DIR/appsettings.json.backup"
        echo "  3. Re-run this script"
        echo ""
        read -p "Continue with existing service configuration? (y/N): " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            echo "Installation cancelled by user."
            # Restore original state
            if [ -f "$INSTALL_DIR/appsettings.json.backup" ]; then
                mv "$INSTALL_DIR/appsettings.json.backup" "$INSTALL_DIR/appsettings.json"
            fi
            exit 1
        fi
    fi
    echo "Restoring existing appsettings.json configuration..."
    mv "$INSTALL_DIR/appsettings.json.backup" "$INSTALL_DIR/appsettings.json"
    echo "Your existing service configuration has been preserved."
elif [ "$SERVICE_EXISTS" = false ]; then
    echo "New installation: Using appsettings.json"
    if ! cmp -s "$CURRENT_DIR/appsettings.json" "/dev/null" 2>/dev/null; then
        echo "Note: Using the appsettings.json from the extracted folder."
    fi
    echo "Please review and edit /opt/conductor-health-check/appsettings.json to configure your settings."
fi

# Ensure the executable has proper permissions
chmod +x $INSTALL_DIR/$EXECUTABLE

# Create or update systemd service file
cat > /etc/systemd/system/$SERVICE_NAME.service << 'SERVICEEOF'
[Unit]
Description=Conductor Health Check Service
After=network.target
StartLimitIntervalSec=0

[Service]
Type=simple
WorkingDirectory=/opt/conductor-health-check
ExecStart=/opt/conductor-health-check/Conductor_Health_Check
Restart=always
RestartSec=10
SyslogIdentifier=conductor-health-check
User=root
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false
Environment=HOME=/root
Environment=DOTNET_CLI_HOME=/root
Environment=DOTNET_ROOT=/usr/share/dotnet
StandardOutput=journal
StandardError=journal
KillSignal=SIGINT
TimeoutStopSec=30

[Install]
WantedBy=multi-user.target
SERVICEEOF

# Reload systemd daemon
systemctl daemon-reload

# Enable the service if it wasn't already enabled
if ! systemctl is-enabled --quiet "$SERVICE_NAME" 2>/dev/null; then
    echo "Enabling service..."
    systemctl enable "$SERVICE_NAME"
fi

# Start the service
echo "Starting service..."
systemctl start "$SERVICE_NAME"

# Wait a moment and check if service started successfully
sleep 2
if systemctl is-active --quiet "$SERVICE_NAME"; then
    if [ "$SERVICE_EXISTS" = true ]; then
        echo "Service updated and started successfully!"
    else
        echo "Service installed and started successfully!"
    fi
else
    echo "Warning: Service may not have started correctly. Check status with: systemctl status $SERVICE_NAME"
fi

echo ""
echo "Management commands:"
echo "  Check status: sudo systemctl status $SERVICE_NAME"
echo "  Start service: sudo systemctl start $SERVICE_NAME"
echo "  Stop service: sudo systemctl stop $SERVICE_NAME"
echo "  Restart service: sudo systemctl restart $SERVICE_NAME"
echo "  View logs: sudo journalctl -u $SERVICE_NAME -f"
echo "  View recent logs: sudo journalctl -u $SERVICE_NAME --since '10 minutes ago'"
echo ""
echo "To uninstall the service:"
echo "  sudo systemctl stop $SERVICE_NAME"
echo "  sudo systemctl disable $SERVICE_NAME"
echo "  sudo rm /etc/systemd/system/$SERVICE_NAME.service"
echo "  sudo rm -rf $INSTALL_DIR"
echo "  sudo systemctl daemon-reload"
echo ""
echo "Installation directory: $INSTALL_DIR"
echo "Service logs: sudo journalctl -u $SERVICE_NAME"
