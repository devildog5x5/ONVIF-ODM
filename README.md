# ONVIF Device Manager

A modern, feature-rich ONVIF Device Manager built with C# and .NET 8. Ships with **two UI editions**:

- **WPF Edition** - Native Windows desktop application with Segoe MDL2 icons
- **Avalonia Edition** - Cross-platform application (Windows, Linux, macOS)

Both editions share the same core business logic, ONVIF protocol services, and MVVM ViewModels.

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

## Requirements

- .NET 8.0 SDK or Runtime
- **WPF Edition**: Windows 10/11
- **Avalonia Edition**: Windows, Linux (X11), or macOS

## Download

Self-contained executables — **no .NET runtime installation required**. Just download, extract, and run.

**Latest Release: v1.3.2** — Built: March 17, 2026 at 1:24 AM UTC

| Platform | Edition | Download | Built |
|----------|---------|----------|-------|
| **Windows x64** | WPF (native) | [OnvifDeviceManager-Wpf-win-x64-v1.3.2.zip](https://github.com/devildog5x5/ONVIF-ODM/releases/download/v1.3.2/OnvifDeviceManager-Wpf-win-x64-v1.3.2.zip) | 2026-03-17 01:24 UTC |
| **Windows x64** | Avalonia | [OnvifDeviceManager-Avalonia-win-x64-v1.3.2.zip](https://github.com/devildog5x5/ONVIF-ODM/releases/download/v1.3.2/OnvifDeviceManager-Avalonia-win-x64-v1.3.2.zip) | 2026-03-17 01:24 UTC |
| **Linux x64** | Avalonia | [OnvifDeviceManager-Avalonia-linux-x64-v1.3.2.tar.gz](https://github.com/devildog5x5/ONVIF-ODM/releases/download/v1.3.2/OnvifDeviceManager-Avalonia-linux-x64-v1.3.2.tar.gz) | 2026-03-17 01:24 UTC |
| **macOS Intel** | Avalonia | [OnvifDeviceManager-Avalonia-osx-x64-v1.3.2.tar.gz](https://github.com/devildog5x5/ONVIF-ODM/releases/download/v1.3.2/OnvifDeviceManager-Avalonia-osx-x64-v1.3.2.tar.gz) | 2026-03-17 01:24 UTC |
| **macOS Apple Silicon** | Avalonia | [OnvifDeviceManager-Avalonia-osx-arm64-v1.3.2.tar.gz](https://github.com/devildog5x5/ONVIF-ODM/releases/download/v1.3.2/OnvifDeviceManager-Avalonia-osx-arm64-v1.3.2.tar.gz) | 2026-03-17 01:24 UTC |

> [See all releases](https://github.com/devildog5x5/ONVIF-ODM/releases)

### Windows Installer

Inno Setup installer scripts are provided in `build/installers/`. To create an installer:

1. Install [Inno Setup 6](https://jrsoftware.org/isinfo.php)
2. Build the executables (see below)
3. Open and compile `build/installers/OnvifDeviceManager-Wpf-Setup.iss` or `OnvifDeviceManager-Avalonia-Setup.iss`

### Linux Installation

```bash
tar -xzf OnvifDeviceManager-Avalonia-linux-x64-v1.0.0.tar.gz
cd OnvifDeviceManager-Avalonia-linux-x64
sudo ./linux-install.sh      # Installs to /opt/onvif-device-manager
onvif-device-manager         # Run from anywhere
sudo ./linux-uninstall.sh    # To uninstall
```

### macOS App Bundle

```bash
# After publishing, create a .app bundle:
./build/packaging/create-macos-app.sh x64     # Intel Mac
./build/packaging/create-macos-app.sh arm64   # Apple Silicon
```

## Building from Source

```bash
# Build the entire solution
dotnet restore
dotnet build

# Run directly (development)
dotnet run --project src/OnvifDeviceManager.Wpf    # WPF (Windows only)
dotnet run --project src/OnvifDeviceManager         # Avalonia (any platform)

# Build all release executables + archives
./build/build-all.sh          # Linux/macOS
.\build\build-all.ps1         # Windows PowerShell
```

## Project Structure

```
OnvifDeviceManager.sln
├── src/OnvifDeviceManager.Core/         # Shared class library
│   ├── Models/                          # Data models
│   │   ├── OnvifDevice.cs               # Core device model
│   │   ├── MediaProfile.cs              # Video/audio profiles
│   │   ├── PtzPreset.cs                 # PTZ preset positions
│   │   ├── DeviceCapabilities.cs        # Service capabilities
│   │   └── NetworkConfiguration.cs      # Network, user, event models
│   ├── Services/                        # ONVIF protocol services
│   │   ├── SoapClient.cs               # SOAP/WS-Security client
│   │   ├── OnvifDiscoveryService.cs    # WS-Discovery
│   │   ├── OnvifDeviceService.cs       # Device management
│   │   ├── OnvifMediaService.cs        # Media profiles & streaming
│   │   └── OnvifPtzService.cs          # PTZ control
│   └── ViewModels/                      # MVVM ViewModels (platform-agnostic)
│       ├── ViewModelBase.cs
│       ├── RelayCommand.cs
│       ├── IUiDispatcher.cs            # Platform abstraction interfaces
│       ├── MainViewModel.cs
│       ├── DiscoveryViewModel.cs
│       ├── DeviceInfoViewModel.cs
│       ├── LiveViewViewModel.cs
│       ├── PtzViewModel.cs
│       ├── ProfilesViewModel.cs
│       ├── NetworkViewModel.cs
│       ├── UsersViewModel.cs
│       ├── EventsViewModel.cs
│       └── SettingsViewModel.cs
│
├── src/OnvifDeviceManager/              # Avalonia UI (cross-platform)
│   ├── Views/                           # .axaml Avalonia views
│   ├── Themes/DarkTheme.axaml
│   ├── Converters/
│   ├── Platform/AvaloniaServices.cs
│   ├── MainWindow.axaml
│   └── App.axaml
│
└── src/OnvifDeviceManager.Wpf/          # WPF UI (Windows native)
    ├── Views/                           # .xaml WPF views
    ├── Themes/DarkTheme.xaml
    ├── Converters/
    ├── Platform/WpfServices.cs
    ├── MainWindow.xaml
    └── App.xaml
```

## Architecture

- **Multi-Project Solution** - Shared Core library with platform-specific UI projects
- **Cross-Platform** - Avalonia UI 11.2 for Windows/Linux/macOS, WPF for Windows-native
- **MVVM Pattern** - Clean separation with ViewModelBase, RelayCommand, AsyncRelayCommand
- **Platform Abstraction** - `IUiDispatcher` and `IClipboardService` interfaces
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
