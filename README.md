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

### Build links

| Source | What | How |
|--------|------|-----|
| **GitHub Release (recommended)** | All published platforms; **permanent `releases/download/…` links** | **[Releases](https://github.com/devildog5x5/ONVIF-ODM/releases)** → **[v2.0.0](https://github.com/devildog5x5/ONVIF-ODM/releases/tag/v2.0.0)** → assets, or **[Latest direct download links](#latest-direct-download-links)** below. |
| **CI** | Windows x64 **WPF** and **Avalonia** only (version + timestamp in each inner filename) | **[Build workflow — runs on `main`](https://github.com/devildog5x5/ONVIF-ODM/actions/workflows/dotnet.yml?query=branch%3Amain)** → latest successful run → **Artifacts** → `ONVIF-ODM-Windows-x64-r{run}-{attempt}` (see also **[Pinned `main` CI build](#pinned-main-ci-build-windows-x64-portable)**). |

**SOP — direct links to latest builds:** A shipping release or hotfix is not complete until maintainers (or automation) provide **verbatim full HTTPS** URLs for each platform ZIP — the canonical place is **[Latest direct download links](#latest-direct-download-links)** in this README (updated when the build stamp changes). `.\create-release-package.ps1` prints the same URLs at the end of each run for copy-paste into README, release notes, and user handoff. Do not rely on “open Releases and find the file” alone when a direct link is expected.

For tags **without** uploaded assets, `releases/download/...` links **404** until the matching `.zip` files are on that release. **v2.0.0** has published assets (see below). CI still produces fresh **Windows x64** pairs after each qualifying push to **`main`** (or **Run workflow** on `workflow_dispatch`).

**Windows (WPF and Avalonia x64):** Extract the **full** ZIP so the **`libvlc`** folder stays **next to** the `.exe`. The single-file `.exe` alone is not enough for embedded live video (LibVLC plugins are loose files).

**Run the right binary name:** Windows x64 packages include **`OnvifDeviceManager.Wpf.exe`** (WPF) or **`OnvifDeviceManager.exe`** (Avalonia). Linux and macOS ZIPs ship an **extensionless** `OnvifDeviceManager` (that is not a Windows `.exe`). If you are on Windows and only see `OnvifDeviceManager` with no extension, you likely downloaded a **Linux/macOS** build, or an old folder — use a **`-win-x64`** ZIP, or re-download from [Releases](https://github.com/devildog5x5/ONVIF-ODM/releases). Release builds run `build/repair-win-apphost.ps1` after each Windows publish so an extensionless PE host is renamed to `.exe` automatically.

**File names include date and time:** Archives produced by `.\build\build-all.ps1` (or `./build/build-all.sh`) and the portable WPF ZIP from `.\create-release-package.ps1` end with `-v{version}-{yyyyMMdd-HHmmss}.zip` (local clock). That stamp is part of the filename so every build is identifiable. **GitHub release links that omit the timestamp** (for example `…-v2.0.0.zip` only) belong to **older** uploads; for current packages, open **[Releases](https://github.com/devildog5x5/ONVIF-ODM/releases)** and choose the asset whose name matches the pattern below.

**Inno Setup** output is `OnvifDeviceManager-Wpf-Setup-{version}-{yyyyMMdd-hhmmss}.exe` (timestamp is applied when you compile the `.iss` file).

**Latest tagged release line:** **v2.0.0** — **build stamp `20260328-113237`** on [GitHub Release v2.0.0](https://github.com/devildog5x5/ONVIF-ODM/releases/tag/v2.0.0). Newer **`main`** commits may use **[Pinned `main` CI build](#pinned-main-ci-build-windows-x64-portable)** for Windows-only CI ZIPs until the next release upload.

**Pinned CI build (Windows x64 portable, `main`):** commit **`9204a85`** — runner stamp **`20260328-172157`** — see **[Pinned `main` CI build](#pinned-main-ci-build-windows-x64-portable)** for the exact **Actions run URL**, **artifact name**, and **inner ZIP file names** (updated whenever maintainers or automation refresh this README after a successful CI pack).

**Source / README refreshed:** 2026-03-28 (v2.0.0 release links, pinned CI, key paths). The `main` branch is verified on every push by **[GitHub Actions — Build workflow](https://github.com/devildog5x5/ONVIF-ODM/actions/workflows/dotnet.yml)** ([workflow file](.github/workflows/dotnet.yml)). On each qualifying **`main`** push (and **manual workflow runs**), that workflow **publishes self-contained Windows x64 WPF + Avalonia ZIPs** and uploads them as **Artifacts** (unless **`[skip ci]`** in the commit message). Open the workflow run → **Artifacts** to download.

| Platform | Edition | Asset name pattern (on [Releases](https://github.com/devildog5x5/ONVIF-ODM/releases)) |
|----------|---------|----------------------------------------------------------------------------------------|
| **Windows x64** | WPF (native) | `OnvifDeviceManager-Wpf-win-x64-v{version}-{yyyyMMdd-HHmmss}.zip` |
| **Windows x64** | Avalonia | `OnvifDeviceManager-Avalonia-win-x64-v{version}-{yyyyMMdd-HHmmss}.zip` |
| **Linux x64** | Avalonia | `OnvifDeviceManager-Avalonia-linux-x64-v{version}-{yyyyMMdd-HHmmss}.tar.gz` (from `build-all.sh`) or `.zip` (from `build-all.ps1` on Windows) |
| **macOS Intel** | Avalonia | `OnvifDeviceManager-Avalonia-osx-x64-v{version}-…` (`.zip` or `.tar.gz` as above) |
| **macOS Apple Silicon** | Avalonia | `OnvifDeviceManager-Avalonia-osx-arm64-v{version}-…` |

> [See all releases](https://github.com/devildog5x5/ONVIF-ODM/releases) · [All workflow runs](https://github.com/devildog5x5/ONVIF-ODM/actions)

### Pinned `main` CI build (Windows x64 portable)

Use this for **latest `main` Windows x64** builds between releases, or when you need the CI artifact bundle. For **all platforms** and **stable links**, prefer **[v2.0.0 release assets](#latest-direct-download-links)**. GitHub does **not** expose a permanent anonymous URL to the artifact ZIP; open the run, sign in if needed, and download **Artifacts**.

| | |
|--|--|
| **Actions run** | [Build workflow run 23690318540](https://github.com/devildog5x5/ONVIF-ODM/actions/runs/23690318540) (success, **push** to `main`, **commit `9204a85`**) |
| **Artifact name** | `ONVIF-ODM-Windows-x64-r30-1` |
| **Portable ZIPs inside** | `OnvifDeviceManager-Wpf-win-x64-v2.0.0-20260328-172157.zip` · `OnvifDeviceManager-Avalonia-win-x64-v2.0.0-20260328-172157.zip` |

**CLI (authenticated):** `gh run download 23690318540 -R devildog5x5/ONVIF-ODM`

**Maintainer SOP:** After each refresh, run **`dotnet build -c Release`**, **`.\build\build-all.ps1`** (or trigger **`gh workflow run dotnet.yml --ref main`**), wait for green, then replace the **run URL**, **artifact name**, **inner ZIP names**, **commit SHA** (the commit **CI checked out** for that run), and **Source / README refreshed** line above. Commit and push the README on `main`. For **README-only** pin fixes after a green run, put **`[skip ci]`** in the commit subject so the workflow (see `.github/workflows/dotnet.yml`) does not enqueue another Windows pack job.

### Latest direct download links

**Target release tag:** **v2.0.0** — **build stamp:** **`20260328-113237`** (from `.\build\build-all.ps1 -Version 2.0.0 -BuildStamp 20260328-113237` used for the release upload).

**SOP:** After each new release upload, replace the table below with **verbatim** full HTTPS `releases/download/…` URLs (same stamp on every row).

| Platform | Edition | Direct link |
|----------|---------|-------------|
| Windows x64 | WPF | [OnvifDeviceManager-Wpf-win-x64-v2.0.0-20260328-113237.zip](https://github.com/devildog5x5/ONVIF-ODM/releases/download/v2.0.0/OnvifDeviceManager-Wpf-win-x64-v2.0.0-20260328-113237.zip) |
| Windows x64 | Avalonia | [OnvifDeviceManager-Avalonia-win-x64-v2.0.0-20260328-113237.zip](https://github.com/devildog5x5/ONVIF-ODM/releases/download/v2.0.0/OnvifDeviceManager-Avalonia-win-x64-v2.0.0-20260328-113237.zip) |
| Linux x64 | Avalonia | [OnvifDeviceManager-Avalonia-linux-x64-v2.0.0-20260328-113237.zip](https://github.com/devildog5x5/ONVIF-ODM/releases/download/v2.0.0/OnvifDeviceManager-Avalonia-linux-x64-v2.0.0-20260328-113237.zip) |
| macOS Intel | Avalonia | [OnvifDeviceManager-Avalonia-osx-x64-v2.0.0-20260328-113237.zip](https://github.com/devildog5x5/ONVIF-ODM/releases/download/v2.0.0/OnvifDeviceManager-Avalonia-osx-x64-v2.0.0-20260328-113237.zip) |
| macOS Apple Silicon | Avalonia | [OnvifDeviceManager-Avalonia-osx-arm64-v2.0.0-20260328-113237.zip](https://github.com/devildog5x5/ONVIF-ODM/releases/download/v2.0.0/OnvifDeviceManager-Avalonia-osx-arm64-v2.0.0-20260328-113237.zip) |

#### Previous release (v1.5.0) — archived direct links

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
unzip OnvifDeviceManager-Avalonia-linux-x64-v2.0.0-*.zip
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
   .\build\build-all.ps1 -Version "2.0.0" -BuildStamp $s
   .\create-release-package.ps1 -Version "2.0.0" -BuildStamp $s
   ```
   Alternatively, remove the `publish/` folder and run only `.\create-release-package.ps1` so it runs `build-all.ps1` internally with a single generated stamp.

3. **Build installer (optional):**  
   Open `build/installers/OnvifDeviceManager-Wpf-Setup.iss` in Inno Setup 6 → Build → Compile → `publish/installers/OnvifDeviceManager-Wpf-Setup-{version}-{yyyyMMdd-hhmmss}.exe`

4. **Create GitHub Release and publish direct links (required):**  
   Tag `v{version}`, upload the ZIP files and setup EXE as release assets.  
   **Direct links to latest builds (required):** Immediately document **verbatim full HTTPS** URLs of the form `https://github.com/devildog5x5/ONVIF-ODM/releases/download/v{version}/<filename>.zip` for **every** platform ZIP you uploaded (WPF Windows, Avalonia Windows, Linux, macOS as applicable). `.\create-release-package.ps1` prints this list at the end of each run — copy it into README **[Latest direct download links](#latest-direct-download-links)**, release notes, and any user handoff.

5. **Update README (links, version, dates/timestamps):**  
   - Keep **[Build links](#build-links)** accurate (CI Artifacts path + when to use Release URLs).  
   - Update **[Latest direct download links](#latest-direct-download-links)** with **full `releases/download/...` URLs**, build stamp, and table rows whenever the stamp or version changes (or note CI-only until upload).  
   - **Date and timestamp updates:** Run the PowerShell snippet under "Key files — last modified" to get current file dates, update the table with those values, and update the "last refreshed" line (date and time) in that section's intro text.

6. **Hotfix / support SOP (required):**  
   For every user-requested fix that is expected to be tested via download, publish a **new timestamped build** and provide the **same verbatim direct URLs** as in README **[Latest direct download links](#latest-direct-download-links)** (Windows WPF + Windows Avalonia, plus Linux/macOS when applicable).  
   Do not ask users to guess which asset is current; always send exact URLs.

### After every new binary build — completion checklist (SOP)

When you finish a **release-style** or **hotfix** build (local `build-all` / `create-release-package`, or CI artifacts you treat as shipping), complete **all** applicable rows and **repeat this list in your handoff** (PR description, release notes, or chat) with each item marked done.

| # | SOP | Done when |
|---|-----|-----------|
| A | **Release Build SOP** §1–2 | `publish/*.zip` exists with one **`yyyyMMdd-HHmmss`** stamp; optional root `OnvifDeviceManager-Wpf-win-x64-v…zip` matches that stamp (`-BuildStamp` shared or `publish/` deleted before `create-release-package.ps1`). |
| B | **Icon Standard Process** | `warrior_icon.ico` regenerated from `branding/master-icon.png` (automatic in `create-release-package.ps1`, or run `.\update-app-icon.ps1`). |
| C | **Release Build SOP** §3 | Inno installer compiled **only if** you are shipping an `.exe` this round (otherwise N/A). |
| D | **Release Build SOP** §4 | Assets uploaded to the correct **GitHub Release** tag; **superseded** ZIPs removed from that tag so “current” files are obvious. |
| E | **Release Build SOP** §5 | README updated: **Build links**, **[Pinned `main` CI build](#pinned-main-ci-build-windows-x64-portable)** (verbatim run URL, artifact name, inner portable ZIP names, commit SHA when CI is the shipping source), **[Latest direct download links](#latest-direct-download-links)** (full HTTPS `releases/download/...` per platform **only** when those assets exist on a Release), **Latest … build stamp** line, **Source / README refreshed** date, **Key files — last modified** table (PowerShell snippet under that table). |
| F | **Hotfix / support SOP** §6 | End user receives the **same verbatim direct URLs** as README **Latest direct download links** (WPF + Avalonia Windows + Linux/macOS if built)—not “get it from Releases.” |
| G | **`dotnet build -c Release`** | Solution builds clean before packaging (no new errors). |

**Agents / automation:** After packaging, output a short **“SOP completion”** block listing A–G with ✅ or N/A so nothing is skipped.

## Project Structure

```
OnvifDeviceManager.sln
├── Directory.Build.props                # Default Version + ApplicationIcon (warrior_icon.ico) for all projects
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

Dates below are **file last-write time** in the maintainer workspace when this section was last refreshed (**2026-03-28 local**). After you pull or edit files, run the snippet under the table to see current dates on your machine. For **last git commit** per path, use: `git log -1 --format=%cs -- <path>`.

| Path | Purpose | Last modified |
|------|---------|---------------|
| `README.md` | Download links, SOPs, key paths | 2026-03-28 (v2.0.0 release URLs) |
| `Directory.Build.props` | Default `Version` + `ApplicationIcon` for repo projects | 2026-03-25 |
| `.github/workflows/dotnet.yml` | CI Release build (Windows runner) | 2026-03-25 21:43 |
| `branding/master-icon.png` | Master icon image | 2026-03-24 16:16 |
| `warrior_icon.ico` | App / window icon | 2026-03-26 12:42 |
| `src/OnvifDeviceManager.Wpf/OnvifDeviceManager.Wpf.csproj` | WPF project (+ LibVLC loose files for single-file) | 2026-03-26 12:38 |
| `src/OnvifDeviceManager.Wpf/MainWindow.xaml` | Main layout | 2026-03-26 12:38 |
| `src/OnvifDeviceManager.Wpf/Views/DiscoveryView.xaml` | Discovery UI / session highlight | 2026-03-23 11:12 |
| `src/OnvifDeviceManager.Wpf/Views/LiveViewView.xaml` | Live view UI | 2026-03-25 15:46 |
| `src/OnvifDeviceManager.Wpf/Views/LiveViewView.xaml.cs` | Live view / LibVLC logic (WPF) | 2026-03-25 15:46 |
| `src/OnvifDeviceManager.Wpf/App.xaml.cs` | Dispatcher / fatal error dialogs | 2026-03-25 21:41 |
| `src/OnvifDeviceManager.Wpf/Converters/Converters.cs` | WPF value converters (incl. snapshot image) | 2026-03-25 16:37 |
| `src/OnvifDeviceManager.Wpf/Converters/DeviceSessionHighlightConverter.cs` | Discovery row highlight (WPF) | 2026-03-23 11:10 |
| `src/OnvifDeviceManager/OnvifDeviceManager.csproj` | Avalonia project + LibVLC + Windows app manifest | 2026-03-26 12:06 |
| `src/OnvifDeviceManager/app.manifest` | Windows `supportedOS` (LibVLC NativeControlHost) | 2026-03-26 12:06 |
| `src/OnvifDeviceManager/MainWindow.axaml` | Avalonia main layout | 2026-03-24 22:44 |
| `src/OnvifDeviceManager/Views/DiscoveryView.axaml` | Discovery UI (Avalonia) | 2026-03-23 11:12 |
| `src/OnvifDeviceManager/Views/LiveViewView.axaml` | Live view (Avalonia) | 2026-03-26 12:38 |
| `src/OnvifDeviceManager/Views/LiveViewView.axaml.cs` | Live view / LibVLC logic (Avalonia) | 2026-03-25 15:46 |
| `src/OnvifDeviceManager/App.axaml.cs` | Avalonia dispatcher / crash logging | 2026-03-25 21:41 |
| `src/OnvifDeviceManager/Converters/DeviceSessionHighlightConverter.cs` | Discovery row highlight (Avalonia) | 2026-03-23 11:10 |
| `src/OnvifDeviceManager.Core/ViewModels/MainViewModel.cs` | Main ViewModel | 2026-03-23 11:08 |
| `src/OnvifDeviceManager.Core/ViewModels/DiscoveryViewModel.cs` | Discovery / active session | 2026-03-25 14:33 |
| `src/OnvifDeviceManager.Core/ViewModels/LiveViewViewModel.cs` | Live view ViewModel | 2026-03-25 14:32 |
| `src/OnvifDeviceManager.Core/ViewModels/ProfilesViewModel.cs` | Media profiles ViewModel | 2026-03-25 14:33 |
| `src/OnvifDeviceManager.Core/Services/OnvifDiscoveryService.cs` | WS-Discovery (UDP receive) | 2026-03-26 12:38 |
| `src/OnvifDeviceManager.Core/Services/OnvifMediaService.cs` | Media / GetStreamUri (incl. RTP-TCP) | 2026-03-25 14:32 |
| `src/OnvifDeviceManager.Core/Services/StreamUriPlayback.cs` | RTSP host normalization for playback | 2026-03-25 14:32 |
| `src/OnvifDeviceManager.Core/Services/OnvifPtzService.cs` | PTZ / ONVIF service | 2026-03-22 22:39 |
| `build/build-all.ps1` | Build script | 2026-03-25 |
| `create-release-package.ps1` | Release packager + SOP echo | 2026-03-25 |

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
  'src/OnvifDeviceManager/OnvifDeviceManager.csproj','src/OnvifDeviceManager/app.manifest',
  'src/OnvifDeviceManager/MainWindow.axaml',
  'src/OnvifDeviceManager/Views/DiscoveryView.axaml','src/OnvifDeviceManager/Views/LiveViewView.axaml',
  'src/OnvifDeviceManager/Views/LiveViewView.axaml.cs',
  'src/OnvifDeviceManager/App.axaml.cs',
  'src/OnvifDeviceManager/Converters/DeviceSessionHighlightConverter.cs',
  'src/OnvifDeviceManager.Core/ViewModels/MainViewModel.cs',
  'src/OnvifDeviceManager.Core/ViewModels/DiscoveryViewModel.cs','src/OnvifDeviceManager.Core/ViewModels/LiveViewViewModel.cs',
  'src/OnvifDeviceManager.Core/ViewModels/ProfilesViewModel.cs',
  'src/OnvifDeviceManager.Core/Services/OnvifDiscoveryService.cs',
  'src/OnvifDeviceManager.Core/Services/OnvifMediaService.cs','src/OnvifDeviceManager.Core/Services/StreamUriPlayback.cs',
  'src/OnvifDeviceManager.Core/Services/OnvifPtzService.cs',
  'build/build-all.ps1','create-release-package.ps1'
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
