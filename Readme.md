# Micser
Micser is a modular audio routing application for Microsoft Windows.

> **Status:** Micser is being rebuilt on .NET 10 with a web-based UI. The target architecture and roadmap are in [docs/Architecture.md](docs/Architecture.md). The previous WPF version is on `master`.

The application consists of:
* an audio engine that runs as a background process in the user's session and exposes a local HTTP API
* a web UI (React) for routing audio graphically using widgets, hosted in a small tray application (WebView2)
* a virtual audio cable driver with a configurable number of devices (WIP, requires an extended validation code signing certificate)

![micser CI](https://github.com/loreggia/micser/workflows/micser%20CI/badge.svg)

## Building
### Requirements
* .NET SDK 10.0.100 or later (see `global.json`)
* Node.js 22.12 or later
* WebView2 Runtime (included in Windows 11)

### Commands
```sh
dotnet build Micser.slnx
dotnet test --solution Micser.slnx

npm install
npm run build          # production build of the web UI (before dotnet publish of the engine, which serves it)
npm run lint
npm run format:check
```

### Running for development
```sh
dotnet run --project src/Engine   # engine on http://127.0.0.1:5080
npm run dev                       # web UI on http://localhost:5173, proxies /api and /hubs to the engine
dotnet run --project src/Shell -- --ui http://localhost:5173   # optional: the tray app showing the dev UI
```

### Driver and installer
The driver (`src/Driver`) and the installer (`src/Installer`) are not part of `Micser.slnx` yet. Building them requires the Windows Driver Kit and the WiX Toolset. Building them in Release mode also requires a code signing certificate named `Certificate.pfx` in a `crt` folder in the repository root.

## Plugins
Audio modules are provided by plugins. Each plugin consists of a .NET project and a web package with the widgets for its modules. The main modules (device input/output, gain, compressor, ...) are provided by `src/Plugins/Main`, which also serves as an example of how to implement a plugin.

## Credits
This project uses the following libraries:
* [NAudio](https://github.com/naudio/NAudio)
* [React](https://react.dev/)
* [Serilog](https://serilog.net/)
* [TUnit](https://github.com/thomhurst/TUnit)
* [NSubstitute](https://nsubstitute.github.io/)
* [Vite](https://vite.dev/)
* [WebView2](https://learn.microsoft.com/microsoft-edge/webview2/)
* [WixSharp](https://github.com/oleg-shilo/wixsharp)
* [Windows Driver Samples](https://github.com/Microsoft/Windows-driver-samples)
