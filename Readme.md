# Micser
Micser is a modular audio routing application for Microsoft Windows.

> **Status:** Micser is being rebuilt on .NET 10 with a web-based UI. The target architecture and roadmap are in [docs/Architecture.md](docs/Architecture.md). The previous WPF version is commit `9386ea4` in the history of `main`.

The application consists of:
* an audio engine that runs as a background process in the user's session and exposes a local HTTP API
* a web UI (React) for routing audio graphically using widgets, hosted in a small tray application (WebView2)
* a virtual audio cable driver with a configurable number of devices (WIP, requires an extended validation code signing certificate)

![micser CI](https://github.com/loreggia/micser/workflows/micser%20CI/badge.svg)

## Building
### Requirements
* .NET SDK 10.0.100 or later (see `global.json`)
* Node.js 24 or later (npm enforces it); with [Volta](https://volta.sh), the pinned version in `package.json` is used
* WebView2 Runtime (included in Windows 11)

### Commands
```sh
dotnet build Micser.slnx
dotnet test --solution Micser.slnx

npm run setup          # npm install, then the allowed install scripts (lavamoat.allowScripts in package.json)
npm run build          # production build of the web UI (before dotnet publish of the engine, which serves it)
npm run lint
npm run format:check
```

### Running for development
With the [Aspire CLI](https://aspire.dev), `aspire start` runs the engine and the web UI with a dashboard for logs and traces; the shell can be started from the dashboard. Without Aspire:
```sh
dotnet run --project src/Engine   # engine on http://127.0.0.1:5080
npm run dev                       # web UI on http://localhost:5173, proxies /api and /hubs to the engine
dotnet run --project src/Shell -- --ui http://localhost:5173   # optional: the tray app showing the dev UI
```

### Packaging
```powershell
./eng/pack.ps1 -Version 0.1.0   # Velopack setup and update packages in artifacts/releases
```
Pushing a tag `vX.Y.Z` builds the release on GitHub Actions and publishes it as a GitHub release, which installed copies update from.

### Driver
The virtual audio cable driver (`src/Vac`) is not part of `Micser.slnx`. It needs Visual Studio with the Windows Driver Kit component and the Spectre-mitigated libraries; the WDK itself comes from NuGet.
```powershell
./eng/build-vac.ps1 -Platform x64,ARM64   # test-signed driver packages in src/Vac/bin
./eng/codeql-vac.ps1                      # Microsoft's CodeQL driver checks
```

## Plugins
Audio modules are provided by plugins. Each plugin consists of a .NET project and a web package with the widgets for its modules. The main modules (device input/output, gain, compressor, ...) are provided by `src/Plugins/Main`, which also serves as an example of how to implement a plugin.

## Credits
This project uses the following libraries:
* [Aspire](https://aspire.dev) (development orchestration)
* [Velopack](https://velopack.io)
* [NAudio](https://github.com/naudio/NAudio)
* [React](https://react.dev/)
* [Serilog](https://serilog.net/)
* [TUnit](https://github.com/thomhurst/TUnit)
* [NSubstitute](https://nsubstitute.github.io/)
* [Vite](https://vite.dev/)
* [WebView2](https://learn.microsoft.com/microsoft-edge/webview2/)
* [WixSharp](https://github.com/oleg-shilo/wixsharp)
* [Windows Driver Samples](https://github.com/Microsoft/Windows-driver-samples)
