# ONVIF Device Manager

A modern, feature-rich ONVIF Device Manager built with C# and WPF (.NET 8). Discover, connect to, and manage ONVIF-compliant IP cameras and devices on your network.

## Features

- **Device Discovery** - Automatically discover ONVIF cameras on your network using WS-Discovery protocol
- **Manual Device Addition** - Add cameras manually by IP address or URL
- **Device Information** - View detailed device information (manufacturer, model, firmware, serial number)
- **Live View** - Capture and display snapshots from camera streams with auto-refresh
- **PTZ Control** - Full pan/tilt/zoom directional controls with adjustable speed
- **PTZ Presets** - Save, recall, and manage PTZ preset positions
- **Media Profiles** - View and manage video/audio encoding profiles and stream URIs
- **Network Configuration** - View device network settings (IP, DHCP, MAC, DNS, ports)
- **User Management** - Create, view, and delete device user accounts
- **Event Monitoring** - Subscribe to and monitor real-time device events
- **ONVIF Security** - WS-Security UsernameToken authentication with digest passwords

## Screenshots

The application features a modern dark theme with:
- Sidebar navigation with categorized sections
- Card-based layout with rounded corners
- Accent color highlighting for interactive elements
- Status bar with connection information

## Requirements

- Windows 10/11
- .NET 8.0 SDK or Runtime
- Network access to ONVIF-compliant cameras

## Building

```bash
# Clone the repository
git clone https://github.com/devildog5x5/ONVIF-ODM.git
cd ONVIF-ODM

# Restore packages and build
dotnet restore
dotnet build

# Run the application
dotnet run --project src/OnvifDeviceManager
```

## Project Structure

```
src/OnvifDeviceManager/
├── Models/                  # Data models
│   ├── OnvifDevice.cs       # Core device model
│   ├── MediaProfile.cs      # Video/audio profiles & encoder configs
│   ├── PtzPreset.cs         # PTZ preset positions
│   ├── DeviceCapabilities.cs # Device capability flags
│   └── NetworkConfiguration.cs # Network, user, event models
├── Services/                # ONVIF protocol services
│   ├── SoapClient.cs        # SOAP/WS-Security client
│   ├── OnvifDiscoveryService.cs  # WS-Discovery implementation
│   ├── OnvifDeviceService.cs     # Device management service
│   ├── OnvifMediaService.cs      # Media profiles & streaming
│   └── OnvifPtzService.cs        # PTZ control service
├── ViewModels/              # MVVM ViewModels
│   ├── ViewModelBase.cs     # Base class with INotifyPropertyChanged
│   ├── RelayCommand.cs      # ICommand implementations
│   ├── MainViewModel.cs     # Main navigation & coordination
│   ├── DiscoveryViewModel.cs # Device discovery & connection
│   ├── DeviceInfoViewModel.cs # Device information display
│   ├── LiveViewViewModel.cs  # Live snapshot viewer
│   ├── PtzViewModel.cs       # PTZ controls & presets
│   ├── ProfilesViewModel.cs  # Media profile management
│   ├── NetworkViewModel.cs   # Network configuration
│   ├── UsersViewModel.cs     # User management
│   ├── EventsViewModel.cs    # Event monitoring
│   └── SettingsViewModel.cs  # Application settings
├── Views/                   # XAML User Controls
│   ├── DiscoveryView.xaml    # Discovery page
│   ├── DeviceInfoView.xaml   # Device info page
│   ├── LiveViewView.xaml     # Live view page
│   ├── PtzView.xaml          # PTZ control page
│   ├── ProfilesView.xaml     # Media profiles page
│   ├── NetworkView.xaml      # Network config page
│   ├── UsersView.xaml        # User management page
│   ├── EventsView.xaml       # Events page
│   └── SettingsView.xaml     # Settings page
├── Converters/              # WPF value converters
├── Themes/                  # UI theme resources
│   └── DarkTheme.xaml       # Dark theme colors & styles
├── MainWindow.xaml          # Main window with sidebar navigation
├── App.xaml                 # Application entry point
└── OnvifDeviceManager.csproj # Project file
```

## Architecture

- **MVVM Pattern** - Clean separation of concerns with ViewModelBase and RelayCommand
- **SOAP/XML** - Direct ONVIF SOAP protocol implementation using HttpClient
- **WS-Discovery** - UDP multicast device discovery per ONVIF specification
- **WS-Security** - Username token authentication with SHA-1 password digest
- **Async/Await** - All network operations are asynchronous

## ONVIF Protocol Support

| Service | Operations |
|---------|-----------|
| Discovery | Probe (WS-Discovery) |
| Device | GetDeviceInformation, GetCapabilities, GetHostname, SetHostname, GetSystemDateAndTime, GetUsers, CreateUsers, DeleteUsers, SystemReboot, SetSystemFactoryDefault |
| Media | GetProfiles, GetStreamUri, GetSnapshotUri, GetVideoEncoderConfigurations |
| PTZ | ContinuousMove, Stop, AbsoluteMove, RelativeMove, GetStatus, GetPresets, GotoPreset, SetPreset, RemovePreset, GotoHomePosition, SetHomePosition |

## License

MIT License - Copyright (c) 2026 Robert Foster
