# ONVIF Device Manager

A modern, feature-rich ONVIF Device Manager built with C# and .NET 8. Ships with **two UI editions**:

- **WPF Edition** - Native Windows desktop application with Segoe MDL2 icons
- **Avalonia Edition** - Cross-platform application (Windows, Linux, macOS)

Both editions share the same core business logic, ONVIF protocol services, and MVVM ViewModels.

## Features

- **Device Discovery** - Automatically discover ONVIF cameras on your network using WS-Discovery protocol
- **Manual Device Addition** - Add cameras manually by IP address or URL
- **Device Information** - View detailed device information (manufacturer, model, firmware, serial number)
- **Live View** - Live RTSP video streaming and snapshots with auto-refresh
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

**Latest Release: v1.5.0** — Built: March 22, 2026

| Platform | Edition | Download | Built |
|----------|---------|----------|-------|
| **Windows x64** | WPF (native) | [OnvifDeviceManager-Wpf-win-x64-v1.5.0.zip](https://github.com/devildog5x5/ONVIF-ODM/releases/download/v1.5.0/OnvifDeviceManager-Wpf-win-x64-v1.5.0.zip) (176 MB) | 2026-03-22 |
| **Windows x64** | Avalonia | [OnvifDeviceManager-Avalonia-win-x64-v1.5.0.zip](https://github.com/devildog5x5/ONVIF-ODM/releases/download/v1.5.0/OnvifDeviceManager-Avalonia-win-x64-v1.5.0.zip) (38 MB) | 2026-03-22 |
| **Linux x64** | Avalonia | [OnvifDeviceManager-Avalonia-linux-x64-v1.5.0.zip](https://github.com/devildog5x5/ONVIF-ODM/releases/download/v1.5.0/OnvifDeviceManager-Avalonia-linux-x64-v1.5.0.zip) (36 MB) | 2026-03-22 |
| **macOS Intel** | Avalonia | [OnvifDeviceManager-Avalonia-osx-x64-v1.5.0.zip](https://github.com/devildog5x5/ONVIF-ODM/releases/download/v1.5.0/OnvifDeviceManager-Avalonia-osx-x64-v1.5.0.zip) (40 MB) | 2026-03-22 |
| **macOS Apple Silicon** | Avalonia | [OnvifDeviceManager-Avalonia-osx-arm64-v1.5.0.zip](https://github.com/devildog5x5/ONVIF-ODM/releases/download/v1.5.0/OnvifDeviceManager-Avalonia-osx-arm64-v1.5.0.zip) (39 MB) | 2026-03-22 |

> [See all releases](https://github.com/devildog5x5/ONVIF-ODM/releases)

### Windows Installer

Inno Setup installer scripts are provided in `build/installers/`. To create an installer:

1. Install [Inno Setup 6](https://jrsoftware.org/isinfo.php)
2. Build the executables (see below)
3. Open and compile `build/installers/OnvifDeviceManager-Wpf-Setup.iss` or `OnvifDeviceManager-Avalonia-Setup.iss`

### Linux Installation

```bash
unzip OnvifDeviceManager-Avalonia-linux-x64-v1.5.0.zip
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

## Release Build SOP

Standard procedure for creating a release:

1. **Publish build:**  
   `.\build\build-all.ps1` — creates self-contained outputs in `publish/` for all platforms.

2. **Create packages:**  
   `.\create-release-package.ps1` — refreshes warrior icon from `branding/master-icon.png`, optionally signs binaries, creates `OnvifDeviceManager-Wpf-win-x64-v{version}.zip` and other archives.

3. **Build installer (optional):**  
   Open `build/installers/OnvifDeviceManager-Wpf-Setup.iss` in Inno Setup 6 → Build → Compile → `publish/installers/OnvifDeviceManager-Wpf-Setup-{version}.exe`

4. **Create GitHub Release:**  
   Tag `v{version}`, upload the ZIP files and setup EXE as release assets.

5. **Update README (links, version, dates/timestamps):**  
   - Update direct download links and version numbers in the Download section.  
   - **Date and timestamp updates:** Run the PowerShell snippet under "Key files — last modified" to get current file dates, update the table with those values, and update the "last refreshed" date (e.g. **2026-03-22**) in that section's intro text.

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

### Key files — last modified (on disk)

Dates below are **file last-write time** in the maintainer workspace when this section was last refreshed (**2026-03-22**). After you pull or edit files, run the snippet under the table to see current dates on your machine. For **last git commit** per path, use: `git log -1 --format=%cs -- <path>`.

| Path | Purpose | Last modified |
|------|---------|---------------|
| `branding/master-icon.png` | Master icon image | 2026-03-22 |
| `warrior_icon.ico` | App / window icon | 2026-03-22 |
| `src/OnvifDeviceManager.Wpf/OnvifDeviceManager.Wpf.csproj` | WPF project | 2026-03-22 |
| `src/OnvifDeviceManager.Wpf/MainWindow.xaml` | Main layout | 2026-03-22 |
| `src/OnvifDeviceManager.Wpf/Views/LiveViewView.xaml` | Live view UI | 2026-03-22 |
| `src/OnvifDeviceManager.Wpf/Views/LiveViewView.xaml.cs` | Live view / LibVLC logic | 2026-03-22 |
| `src/OnvifDeviceManager.Core/ViewModels/MainViewModel.cs` | Main ViewModel | 2026-03-22 |
| `src/OnvifDeviceManager.Core/ViewModels/LiveViewViewModel.cs` | Live view ViewModel | 2026-03-22 |
| `src/OnvifDeviceManager.Core/Services/OnvifPtzService.cs` | PTZ / ONVIF service | 2026-03-22 |
| `build/build-all.ps1` | Build script | 2026-03-22 |
| `create-release-package.ps1` | Release packager | 2026-03-22 |

**Refresh dates locally (PowerShell, from repo root):**

```powershell
$paths = @(
  'branding/master-icon.png','warrior_icon.ico',
  'src/OnvifDeviceManager.Wpf/OnvifDeviceManager.Wpf.csproj',
  'src/OnvifDeviceManager.Wpf/MainWindow.xaml',
  'src/OnvifDeviceManager.Wpf/Views/LiveViewView.xaml','src/OnvifDeviceManager.Wpf/Views/LiveViewView.xaml.cs',
  'src/OnvifDeviceManager.Core/ViewModels/MainViewModel.cs','src/OnvifDeviceManager.Core/ViewModels/LiveViewViewModel.cs',
  'src/OnvifDeviceManager.Core/Services/OnvifPtzService.cs',
  'build/build-all.ps1','create-release-package.ps1'
)
$paths | ForEach-Object { if (Test-Path $_) { '{0}  {1}' -f ((Get-Item $_).LastWriteTime.ToString('yyyy-MM-dd')), $_ } }
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

## Icon Standard Process

All application and installer icon usage in this repo uses the warrior icon:

- Source image: `branding/master-icon.png`
- Generated icon: `warrior_icon.ico`
- Refresh command: `powershell -ExecutionPolicy Bypass -File .\update-app-icon.ps1`

The release script (`create-release-package.ps1`) runs this icon refresh step before packaging.

## License

MIT License - Copyright (c) 2026 Robert Foster
