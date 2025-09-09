# Conductor Health Check

A cross-platform .NET 9 application that monitors VPN gateway connectivity and sends SMS alerts when the connection fails. The application runs as a background service and checks connectivity every 20 minutes.

## Features

- **Cross-platform support**: Runs on Windows and Linux
- **Automated VPN monitoring**: Pings gateway every 20 minutes
- **SMS notifications**: Sends alerts via Dexatel API when connection fails
- **Rate limiting**: Prevents spam by limiting alerts to once per 24 hours
- **Web interface**: Swagger UI for API documentation and testing
- **Health checks**: Built-in health endpoint for monitoring
- **Logging**: Comprehensive logging with configurable levels
- **Service installation**: Easy Linux systemd service installation

## Architecture

The application consists of several components:

- **Conductor_Health_Check**: Main web API application
- **Bash**: Linux command execution wrapper
- **PowerShell**: Windows command execution wrapper  
- **Broker**: SMS notification service using Dexatel API

## Quick Start

### Download Pre-built Binaries

1. Go to the [Releases](../../releases) page
2. Download the appropriate file for your platform:
   - **Windows**: `conductor-health-check-windows-*.zip`
   - **Linux**: `conductor-health-check-linux-*.tar.gz`

### Windows Installation

1. Extract the downloaded ZIP file
2. Edit `appsettings.json` to configure your settings (see Configuration section)
3. Run `Conductor_Health_Check.exe`
4. Access the web interface at `http://localhost:7070`

### Linux Installation

#### Option 1: Run Manually
1. Extract the downloaded tar.gz file
2. Make the executable runnable: `chmod +x Conductor_Health_Check`
3. Edit `appsettings.json` to configure your settings
4. Run `./Conductor_Health_Check`

#### Option 2: Install as System Service (Recommended)
1. Extract the downloaded tar.gz file
2. Edit `appsettings.json` to configure your settings (optional - can be done after installation)
3. Make the install script executable: `chmod +x install-service.sh`
4. Run the installation script: `sudo ./install-service.sh`

The service will be installed to `/opt/conductor-health-check/` and will start automatically.

## Configuration

Edit the `appsettings.json` file to configure the application:

```json
{
  "GatewayIP": "192.168.10.2",           // IP address of your VPN gateway
  "LogFilePath": "conductor_health.log",  // Path to log file
  "ServerName": "My Server Name",         // Name used in SMS alerts
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:7070"      // Web interface URL and port
      }
    }
  },
  "dexatel": {
    "url": "https://api.dexatel.com",     // Dexatel API endpoint
    "token": "your-api-token-here",       // Your Dexatel API token
    "to": ["+1234567890", "+0987654321"], // Phone numbers to receive alerts
    "from": "YourSenderID"                // Sender ID for SMS messages
  }
}
```

### Required Configuration

Before running the application, you must configure:

1. **GatewayIP**: The IP address of your VPN gateway to monitor
2. **ServerName**: A friendly name for your server (used in SMS alerts)
3. **Dexatel settings**: 
   - `token`: Your Dexatel API token
   - `to`: Array of phone numbers to receive alerts
   - `from`: Your sender ID

## Service Management (Linux)

If you installed as a systemd service, use these commands:

```bash
# Check service status
sudo systemctl status conductor-health-check

# Start the service
sudo systemctl start conductor-health-check

# Stop the service
sudo systemctl stop conductor-health-check

# Restart the service
sudo systemctl restart conductor-health-check

# View logs
sudo journalctl -u conductor-health-check -f

# View recent logs
sudo journalctl -u conductor-health-check --since '10 minutes ago'
```

## API Endpoints

Once running, the application provides several endpoints:

- **Swagger UI**: `http://localhost:7070/swagger` - Interactive API documentation
- **Health Check**: `http://localhost:7070/health` - Application health status
- **Logs**: `http://localhost:7070/api/logs` - View application logs

## How It Works

1. **Startup**: The application starts a background timer service
2. **Scheduling**: Checks are scheduled to run every 20 minutes, aligned to :00, :20, :40 minutes past the hour
3. **Connectivity Test**: 
   - **Windows**: Uses PowerShell `Test-Connection` command
   - **Linux**: Uses `ping -c 4` command
4. **Failure Handling**: If the ping fails:
   - Logs the failure
   - Sends SMS alert (max once per 24 hours)
   - Continues monitoring
5. **Success**: Logs successful connections

## Building from Source

### Prerequisites

- .NET 9 SDK
- Git

### Build Commands

```bash
# Clone the repository
git clone <repository-url>
cd Conductor_Health_Check

# Restore dependencies
dotnet restore

# Run tests
dotnet test

# Run locally
dotnet run --project Conductor_Health_Check

# Build for release
dotnet publish Conductor_Health_Check/Conductor_Health_Check.csproj \
  --configuration Release \
  --runtime win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true

# For Linux
dotnet publish Conductor_Health_Check/Conductor_Health_Check.csproj \
  --configuration Release \
  --runtime linux-x64 \
  --self-contained true \
  -p:PublishSingleFile=true
```

## Updating the Service

### Windows
1. Stop the running application
2. Download and extract the new release
3. Replace the old files with new ones (preserve your `appsettings.json`)
4. Start the application

### Linux (Service Installation)
1. Download and extract the new release
2. Run `sudo ./install-service.sh` again
   - The script will automatically stop the service, update files, and restart
   - Your existing `appsettings.json` configuration will be preserved

## Troubleshooting

### Common Issues

1. **Permission Denied (Linux)**:
   ```bash
   chmod +x Conductor_Health_Check
   chmod +x install-service.sh
   ```

2. **Port Already in Use**:
   - Change the port in `appsettings.json` under `Kestrel:Endpoints:Http:Url`

3. **SMS Not Sending**:
   - Verify your Dexatel API token and configuration
   - Check logs for API errors
   - Ensure phone numbers include country codes

4. **Service Won't Start (Linux)**:
   ```bash
   # Check service status
   sudo systemctl status conductor-health-check
   
   # Check logs
   sudo journalctl -u conductor-health-check --since '5 minutes ago'
   
   # Verify file permissions
   sudo chmod +x /opt/conductor-health-check/Conductor_Health_Check
   ```

### Log Locations

- **Manual run**: `conductor_health.log` in the application directory
- **Linux service**: Use `journalctl` commands shown above
- **Configuration file**: Check `LogFilePath` setting in `appsettings.json`

## Uninstalling

### Windows
Simply delete the application folder.

### Linux Service
```bash
sudo systemctl stop conductor-health-check
sudo systemctl disable conductor-health-check
sudo rm /etc/systemd/system/conductor-health-check.service
sudo rm -rf /opt/conductor-health-check
sudo systemctl daemon-reload
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

For issues, feature requests, or questions:

1. Check the logs for error messages
2. Review the configuration settings
3. Create an issue in the GitHub repository

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## Version History

See [Releases](../../releases) for version history and changelog.
