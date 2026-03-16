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

## Building & Running

```bash
# Restore and build the entire solution (all 3 projects)
dotnet restore
dotnet build

# Run the WPF edition (Windows only)
dotnet run --project src/OnvifDeviceManager.Wpf

# Run the Avalonia edition (any platform)
dotnet run --project src/OnvifDeviceManager
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
