# AGENTS.md

## Cursor Cloud specific instructions

### Project overview

ONVIF Device Manager — a .NET 8.0 / C# desktop app for managing ONVIF IP cameras. Two UI editions exist: Avalonia (cross-platform) and WPF (Windows-only). On Linux only the Avalonia edition can be built and run. No test projects or lint tooling exist in the repo.

### Prerequisites

- **.NET 8.0 SDK** installed at `$HOME/.dotnet` with `DOTNET_ROOT` and `PATH` configured in `~/.bashrc`.
- **Xvfb** (or another X11 display server) must be running for the Avalonia GUI. The VM typically has Xvfb on `:1` already.

### Build and run (Avalonia edition on Linux)

See `README.md` "Building from Source" section. Key commands:

```
dotnet restore src/OnvifDeviceManager/OnvifDeviceManager.csproj
dotnet build src/OnvifDeviceManager/OnvifDeviceManager.csproj
DISPLAY=:1 dotnet run --project src/OnvifDeviceManager
```

### Caveats

- The WPF project (`OnvifDeviceManager.Wpf`) targets `net8.0-windows` and **cannot build on Linux**. Do not run `dotnet build` on the full solution — build only the Avalonia project or the Core library.
- There are **no automated tests** (no test projects in the solution). Verification is done by building and running the app manually.
- There is **no linting configuration** (no `.editorconfig` enforcement, no analyzers). Code style is not enforced by tooling.
- The app requires ONVIF cameras on the network for full E2E testing. Without cameras, you can still verify the UI launches, navigation works, and the discovery scan completes (finding 0 devices).
- On first `dotnet run`, the build step runs inline. Subsequent runs are faster since binaries are cached.
