# ONVIF Device Manager

[![Build](https://github.com/devildog5x5/ONVIF-ODM/actions/workflows/dotnet.yml/badge.svg)](https://github.com/devildog5x5/ONVIF-ODM/actions/workflows/dotnet.yml)

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

**Windows (WPF and Avalonia x64):** Extract the **full** ZIP so the **`libvlc`** folder stays **next to** the `.exe`. The single-file `.exe` alone is not enough for embedded live video (LibVLC plugins are loose files).

**Run the right binary name:** Windows x64 packages include **`OnvifDeviceManager.Wpf.exe`** (WPF) or **`OnvifDeviceManager.exe`** (Avalonia). Linux and macOS ZIPs ship an **extensionless** `OnvifDeviceManager` (that is not a Windows `.exe`). If you are on Windows and only see `OnvifDeviceManager` with no extension, you likely downloaded a **Linux/macOS** build, or an old folder — use a **`-win-x64`** ZIP, or re-download from [Releases](https://github.com/devildog5x5/ONVIF-ODM/releases). Release builds run `build/repair-win-apphost.ps1` after each Windows publish so an extensionless PE host is renamed to `.exe` automatically.

**File names include date and time:** Archives produced by `.\build\build-all.ps1` (or `./build/build-all.sh`) and the portable WPF ZIP from `.\create-release-package.ps1` end with `-v{version}-{yyyyMMdd-HHmmss}.zip` (local clock). That stamp is part of the filename so every build is identifiable. **GitHub release links that omit the timestamp** (for example `…-v1.5.0.zip` only) belong to **older** uploads; for current packages, open **[Releases](https://github.com/devildog5x5/ONVIF-ODM/releases)** and choose the asset whose name matches the pattern below.

**Inno Setup** output is `OnvifDeviceManager-Wpf-Setup-{version}-{yyyyMMdd-hhmmss}.exe` (timestamp is applied when you compile the `.iss` file).

**Latest tagged release line:** v1.5.0 — **current published ZIPs use build stamp `20260325-222521`** (see [direct links](#direct-download-links-20260325-222521) below and [Releases](https://github.com/devildog5x5/ONVIF-ODM/releases)).

**Source / README refreshed:** 2026-03-25 22:28 local (documentation and key paths below). The `main` branch is verified on every push by **[GitHub Actions — Build workflow](https://github.com/devildog5x5/ONVIF-ODM/actions/workflows/dotnet.yml)** ([workflow file](.github/workflows/dotnet.yml)). On each **`main`** push (and **manual workflow runs**), that workflow also **publishes self-contained Windows x64 WPF + Avalonia ZIPs** and uploads them as **Artifacts**; each ZIP file name includes **`v{Version}-{yyyyMMdd-HHmmss}`** (runner local time). Open the workflow run → **Artifacts** to download.

| Platform | Edition | Asset name pattern (on [Releases](https://github.com/devildog5x5/ONVIF-ODM/releases)) |
|----------|---------|----------------------------------------------------------------------------------------|
| **Windows x64** | WPF (native) | `OnvifDeviceManager-Wpf-win-x64-v{version}-{yyyyMMdd-HHmmss}.zip` |
| **Windows x64** | Avalonia | `OnvifDeviceManager-Avalonia-win-x64-v{version}-{yyyyMMdd-HHmmss}.zip` |
| **Linux x64** | Avalonia | `OnvifDeviceManager-Avalonia-linux-x64-v{version}-{yyyyMMdd-HHmmss}.tar.gz` (from `build-all.sh`) or `.zip` (from `build-all.ps1` on Windows) |
| **macOS Intel** | Avalonia | `OnvifDeviceManager-Avalonia-osx-x64-v{version}-…` (`.zip` or `.tar.gz` as above) |
| **macOS Apple Silicon** | Avalonia | `OnvifDeviceManager-Avalonia-osx-arm64-v{version}-…` |

> [See all releases](https://github.com/devildog5x5/ONVIF-ODM/releases) · [All workflow runs](https://github.com/devildog5x5/ONVIF-ODM/actions)

### Direct download links (20260325-222521)

Use these **exact** URLs when sharing builds (hotfix / support SOP). After you publish newer timestamped assets, update this block.

| Platform | Asset | Direct link |
|----------|-------|-------------|
| Windows x64 | WPF | [OnvifDeviceManager-Wpf-win-x64-v1.5.0-20260325-222521.zip](https://github.com/devildog5x5/ONVIF-ODM/releases/download/v1.5.0/OnvifDeviceManager-Wpf-win-x64-v1.5.0-20260325-222521.zip) |
| Windows x64 | Avalonia | [OnvifDeviceManager-Avalonia-win-x64-v1.5.0-20260325-222521.zip](https://github.com/devildog5x5/ONVIF-ODM/releases/download/v1.5.0/OnvifDeviceManager-Avalonia-win-x64-v1.5.0-20260325-222521.zip) |
| Linux x64 | Avalonia | [OnvifDeviceManager-Avalonia-linux-x64-v1.5.0-20260325-222521.zip](https://github.com/devildog5x5/ONVIF-ODM/releases/download/v1.5.0/OnvifDeviceManager-Avalonia-linux-x64-v1.5.0-20260325-222521.zip) |
| macOS Intel | Avalonia | [OnvifDeviceManager-Avalonia-osx-x64-v1.5.0-20260325-222521.zip](https://github.com/devildog5x5/ONVIF-ODM/releases/download/v1.5.0/OnvifDeviceManager-Avalonia-osx-x64-v1.5.0-20260325-222521.zip) |
| macOS Apple Silicon | Avalonia | [OnvifDeviceManager-Avalonia-osx-arm64-v1.5.0-20260325-222521.zip](https://github.com/devildog5x5/ONVIF-ODM/releases/download/v1.5.0/OnvifDeviceManager-Avalonia-osx-arm64-v1.5.0-20260325-222521.zip) |

To produce matching binaries locally from source, follow [Release Build SOP](#release-build-sop) (use the **same** `-BuildStamp` for `build-all.ps1` and `create-release-package.ps1`, or delete `publish/` and run `create-release-package.ps1` alone so one stamp is used end-to-end).

### Windows Installer

Inno Setup installer scripts are provided in `build/installers/`. To create an installer:

1. Install [Inno Setup 6](https://jrsoftware.org/isinfo.php)
2. Build the executables (see below)
3. Open and compile `build/installers/OnvifDeviceManager-Wpf-Setup.iss` or `OnvifDeviceManager-Avalonia-Setup.iss`

### Linux Installation

```bash
unzip OnvifDeviceManager-Avalonia-linux-x64-v1.5.0-*.zip
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

# Quick local self-contained previews (output under publish/, gitignored) — each folder contains the .exe plus warrior_icon.ico
dotnet publish src/OnvifDeviceManager.Wpf/OnvifDeviceManager.Wpf.csproj -c Release -r win-x64 -o publish/preview-executables/OnvifDeviceManager-Wpf-win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish src/OnvifDeviceManager/OnvifDeviceManager.csproj -c Release -r win-x64 -o publish/preview-executables/OnvifDeviceManager-Avalonia-win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

## Release Build SOP

Standard procedure for creating a release:

1. **Publish build:**  
   `.\build\build-all.ps1` — creates self-contained outputs in `publish/` for all platforms and ZIPs named `…-v{version}-{yyyyMMdd-HHmmss}.zip`.

2. **Create packages:**  
   `.\create-release-package.ps1` — refreshes warrior icon from `branding/master-icon.png` ([Icon Standard Process](#icon-standard-process)), optionally signs binaries, creates `OnvifDeviceManager-Wpf-win-x64-v{version}-{yyyyMMdd-HHmmss}.zip` at the repo root (and lists `publish/*.zip`).  
   **Same build stamp everywhere:** pass one stamp to both scripts, for example:
   ```powershell
   $s = Get-Date -Format "yyyyMMdd-HHmmss"
   .\build\build-all.ps1 -Version "1.5.0" -BuildStamp $s
   .\create-release-package.ps1 -Version "1.5.0" -BuildStamp $s
   ```
   Alternatively, remove the `publish/` folder and run only `.\create-release-package.ps1` so it runs `build-all.ps1` internally with a single generated stamp.

3. **Build installer (optional):**  
   Open `build/installers/OnvifDeviceManager-Wpf-Setup.iss` in Inno Setup 6 → Build → Compile → `publish/installers/OnvifDeviceManager-Wpf-Setup-{version}-{yyyyMMdd-hhmmss}.exe`

4. **Create GitHub Release and publish direct links (required):**  
   Tag `v{version}`, upload the ZIP files and setup EXE as release assets.  
   Then publish a short link block (README/changelog/release notes) with **direct URLs** to each uploaded asset for all projects/platforms.

5. **Update README (links, version, dates/timestamps):**  
   - Update direct download links and version numbers in the Download section.  
   - **Date and timestamp updates:** Run the PowerShell snippet under "Key files — last modified" to get current file dates, update the table with those values, and update the "last refreshed" line (date and time) in that section's intro text.

6. **Hotfix / support SOP (required):**  
   For every user-requested fix that is expected to be tested via download, publish a **new timestamped build** and provide the user a **direct link set** (Windows WPF + Windows Avalonia, plus Linux/macOS when applicable).  
   Do not ask users to guess which asset is current; always send exact URLs.

### After every new binary build — completion checklist (SOP)

When you finish a **release-style** or **hotfix** build (local `build-all` / `create-release-package`, or CI artifacts you treat as shipping), complete **all** applicable rows and **repeat this list in your handoff** (PR description, release notes, or chat) with each item marked done.

| # | SOP | Done when |
|---|-----|-----------|
| A | **Release Build SOP** §1–2 | `publish/*.zip` exists with one **`yyyyMMdd-HHmmss`** stamp; optional root `OnvifDeviceManager-Wpf-win-x64-v…zip` matches that stamp (`-BuildStamp` shared or `publish/` deleted before `create-release-package.ps1`). |
| B | **Icon Standard Process** | `warrior_icon.ico` regenerated from `branding/master-icon.png` (automatic in `create-release-package.ps1`, or run `.\update-app-icon.ps1`). |
| C | **Release Build SOP** §3 | Inno installer compiled **only if** you are shipping an `.exe` this round (otherwise N/A). |
| D | **Release Build SOP** §4 | Assets uploaded to the correct **GitHub Release** tag; **superseded** ZIPs removed from that tag so “current” files are obvious. |
| E | **Release Build SOP** §5 | README updated: **Latest … build stamp**, **Direct download links** table (`releases/download/...` URLs), **Source / README refreshed** date, **Key files — last modified** table (run the PowerShell snippet under that table). |
| F | **Hotfix / support SOP** §6 | End user receives **verbatim direct URLs** for WPF + Avalonia Windows (and Linux/macOS if you built them)—not “get it from Releases.” |
| G | **`dotnet build -c Release`** | Solution builds clean before packaging (no new errors). |

**Agents / automation:** After packaging, output a short **“SOP completion”** block listing A–G with ✅ or N/A so nothing is skipped.

## Project Structure

```
OnvifDeviceManager.sln
├── Directory.Build.props                # Default ApplicationIcon (warrior_icon.ico) for all projects
├── .github/workflows/dotnet.yml         # CI: dotnet build Release on push/PR
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
│   ├── Converters/                      # Includes DeviceSessionHighlightConverter.cs
│   ├── Platform/AvaloniaServices.cs
│   ├── MainWindow.axaml
│   └── App.axaml
│
└── src/OnvifDeviceManager.Wpf/          # WPF UI (Windows native)
    ├── Views/                           # .xaml WPF views
    ├── Themes/DarkTheme.xaml
    ├── Converters/                      # Includes DeviceSessionHighlightConverter.cs
    ├── Platform/WpfServices.cs
    ├── MainWindow.xaml
    └── App.xaml
```

### Key files — last modified (on disk)

Dates below are **file last-write time** in the maintainer workspace when this section was last refreshed (**2026-03-25 22:28 local**). After you pull or edit files, run the snippet under the table to see current dates on your machine. For **last git commit** per path, use: `git log -1 --format=%cs -- <path>`.

| Path | Purpose | Last modified |
|------|---------|---------------|
| `README.md` | Download links, SOPs, key paths | 2026-03-25 22:30 |
| `Directory.Build.props` | Default `ApplicationIcon` for repo projects | 2026-03-23 11:08 |
| `.github/workflows/dotnet.yml` | CI Release build (Windows runner) | 2026-03-25 21:43 |
| `branding/master-icon.png` | Master icon image | 2026-03-24 16:16 |
| `warrior_icon.ico` | App / window icon | 2026-03-25 22:28 |
| `src/OnvifDeviceManager.Wpf/OnvifDeviceManager.Wpf.csproj` | WPF project (+ LibVLC loose files for single-file) | 2026-03-25 16:44 |
| `src/OnvifDeviceManager.Wpf/MainWindow.xaml` | Main layout | 2026-03-24 22:43 |
| `src/OnvifDeviceManager.Wpf/Views/DiscoveryView.xaml` | Discovery UI / session highlight | 2026-03-23 11:12 |
| `src/OnvifDeviceManager.Wpf/Views/LiveViewView.xaml` | Live view UI | 2026-03-25 15:46 |
| `src/OnvifDeviceManager.Wpf/Views/LiveViewView.xaml.cs` | Live view / LibVLC logic (WPF) | 2026-03-25 15:46 |
| `src/OnvifDeviceManager.Wpf/App.xaml.cs` | Dispatcher / fatal error dialogs | 2026-03-25 21:41 |
| `src/OnvifDeviceManager.Wpf/Converters/Converters.cs` | WPF value converters (incl. snapshot image) | 2026-03-25 16:37 |
| `src/OnvifDeviceManager.Wpf/Converters/DeviceSessionHighlightConverter.cs` | Discovery row highlight (WPF) | 2026-03-23 11:10 |
| `src/OnvifDeviceManager/OnvifDeviceManager.csproj` | Avalonia project + LibVLC loose files (Windows) | 2026-03-25 16:44 |
| `src/OnvifDeviceManager/MainWindow.axaml` | Avalonia main layout | 2026-03-24 22:44 |
| `src/OnvifDeviceManager/Views/DiscoveryView.axaml` | Discovery UI (Avalonia) | 2026-03-23 11:12 |
| `src/OnvifDeviceManager/Views/LiveViewView.axaml` | Live view (Avalonia) | 2026-03-24 22:39 |
| `src/OnvifDeviceManager/Views/LiveViewView.axaml.cs` | Live view / LibVLC logic (Avalonia) | 2026-03-25 15:46 |
| `src/OnvifDeviceManager/App.axaml.cs` | Avalonia dispatcher / error UI | 2026-03-25 21:41 |
| `src/OnvifDeviceManager/Converters/Converters.cs` | Avalonia converters (incl. snapshot bitmap) | 2026-03-25 21:41 |
| `src/OnvifDeviceManager/Converters/DeviceSessionHighlightConverter.cs` | Discovery row highlight (Avalonia) | 2026-03-23 11:10 |
| `src/OnvifDeviceManager.Core/ViewModels/MainViewModel.cs` | Main ViewModel | 2026-03-23 11:08 |
| `src/OnvifDeviceManager.Core/ViewModels/DiscoveryViewModel.cs` | Discovery / active session | 2026-03-25 14:33 |
| `src/OnvifDeviceManager.Core/ViewModels/LiveViewViewModel.cs` | Live view ViewModel | 2026-03-25 14:32 |
| `src/OnvifDeviceManager.Core/ViewModels/ProfilesViewModel.cs` | Media profiles ViewModel | 2026-03-25 14:33 |
| `src/OnvifDeviceManager.Core/Services/OnvifMediaService.cs` | Media / GetStreamUri (incl. RTP-TCP) | 2026-03-25 14:32 |
| `src/OnvifDeviceManager.Core/Services/StreamUriPlayback.cs` | RTSP host normalization for playback | 2026-03-25 14:32 |
| `src/OnvifDeviceManager.Core/Services/OnvifPtzService.cs` | PTZ / ONVIF service | 2026-03-22 22:39 |
| `src/OnvifDeviceManager.Core/Services/CrashLogger.cs` | Crash log + exception summary for dialogs | 2026-03-25 21:41 |
| `build/build-all.ps1` | Build script (+ Windows apphost repair hook) | 2026-03-25 21:43 |
| `build/build-all.sh` | Build script (Unix; Windows apphost repair) | 2026-03-25 21:44 |
| `build/repair-win-apphost.ps1` | Ensures `*.exe` name for Windows single-file publish | 2026-03-25 21:43 |
| `create-release-package.ps1` | Release packager + SOP echo | 2026-03-25 21:43 |

**Refresh dates locally (PowerShell, from repo root):**

```powershell
$paths = @(
  'README.md',
  'Directory.Build.props','.github/workflows/dotnet.yml',
  'branding/master-icon.png','warrior_icon.ico',
  'src/OnvifDeviceManager.Wpf/OnvifDeviceManager.Wpf.csproj',
  'src/OnvifDeviceManager.Wpf/MainWindow.xaml',
  'src/OnvifDeviceManager.Wpf/Views/DiscoveryView.xaml',
  'src/OnvifDeviceManager.Wpf/Views/LiveViewView.xaml','src/OnvifDeviceManager.Wpf/Views/LiveViewView.xaml.cs',
  'src/OnvifDeviceManager.Wpf/App.xaml.cs',
  'src/OnvifDeviceManager.Wpf/Converters/Converters.cs',
  'src/OnvifDeviceManager.Wpf/Converters/DeviceSessionHighlightConverter.cs',
  'src/OnvifDeviceManager/OnvifDeviceManager.csproj','src/OnvifDeviceManager/MainWindow.axaml',
  'src/OnvifDeviceManager/Views/DiscoveryView.axaml','src/OnvifDeviceManager/Views/LiveViewView.axaml',
  'src/OnvifDeviceManager/Views/LiveViewView.axaml.cs',
  'src/OnvifDeviceManager/App.axaml.cs',
  'src/OnvifDeviceManager/Converters/Converters.cs',
  'src/OnvifDeviceManager/Converters/DeviceSessionHighlightConverter.cs',
  'src/OnvifDeviceManager.Core/ViewModels/MainViewModel.cs',
  'src/OnvifDeviceManager.Core/ViewModels/DiscoveryViewModel.cs','src/OnvifDeviceManager.Core/ViewModels/LiveViewViewModel.cs',
  'src/OnvifDeviceManager.Core/ViewModels/ProfilesViewModel.cs',
  'src/OnvifDeviceManager.Core/Services/OnvifMediaService.cs','src/OnvifDeviceManager.Core/Services/StreamUriPlayback.cs',
  'src/OnvifDeviceManager.Core/Services/OnvifPtzService.cs',
  'src/OnvifDeviceManager.Core/Services/CrashLogger.cs',
  'build/build-all.ps1','build/build-all.sh','build/repair-win-apphost.ps1','create-release-package.ps1'
)
$paths | ForEach-Object { if (Test-Path $_) { '{0}  {1}' -f ((Get-Item $_).LastWriteTime.ToString('yyyy-MM-dd HH:mm:ss')), $_ } }
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

All application and installer icon usage in this repo uses the warrior icon (`warrior_icon.ico`, generated from the same artwork as `branding/master-icon.png`):

| Where | How |
|--------|-----|
| **Repo default** | `Directory.Build.props` sets `ApplicationIcon` when a project does not override it. |
| **WPF** | `ApplicationIcon` + embedded `Resource`; `App.xaml` default `Window.Icon`; `MainWindow.xaml` `Icon` + title bar `Image`; **published folder** includes `warrior_icon.ico` beside the `.exe` (`ExcludeFromSingleFile`) for shortcuts/installers. |
| **Avalonia** | `ApplicationIcon`; `AvaloniaResource` `Assets/warrior_icon.ico` for `MainWindow` `Icon` + title `Image`; **published folder** includes `warrior_icon.ico` beside the `.exe`. |
| **Inno Setup (WPF + Avalonia)** | `SetupIconFile=..\..\warrior_icon.ico`; Start Menu / desktop `[Icons]` use `IconFilename: "{app}\warrior_icon.ico"`. |
| **Linux** | `.desktop` `Icon=/opt/onvif-device-manager/warrior_icon.ico` (`linux-install.sh` + `build/packaging/onvif-device-manager.desktop`). |
| **macOS** | `build/packaging/create-macos-app.sh` builds `AppIcon.icns` from `branding/master-icon.png` (same pixels as the warrior brand) and sets `CFBundleIconFile` when `sips` + `iconutil` are available. |

- **Refresh command:** `powershell -ExecutionPolicy Bypass -File .\update-app-icon.ps1`

The release script (`create-release-package.ps1`) runs this icon refresh step before packaging.

**If the taskbar or desktop shortcut still shows a generic icon:** run `.\update-app-icon.ps1`, rebuild or republish, then start the app from the **published `.exe`** (not `dotnet run`). Delete an old shortcut and create a new one from the executable so Windows picks up the embedded icon. The in-app title bar uses **`branding/master-icon.png`** (with alpha) for a crisp image; the **`.exe`** uses **`warrior_icon.ico`** (multi-resolution, alpha preserved).

## License

MIT License - Copyright (c) 2026 Robert Foster
