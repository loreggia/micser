# Micser

Micser is a modular audio routing application for Microsoft Windows.

The application consists of:

- an audio engine that runs as a background process in the user's session and exposes a local HTTP API
- a web UI (React) for routing audio graphically using widgets, hosted in a small tray application (WebView2)
- a virtual audio cable driver with a configurable number of devices (WIP, still requires an extended validation code signing certificate)

[![micser CI](https://github.com/loreggia/micser/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/loreggia/micser/actions/workflows/ci.yml?query=branch%3Amain)

## Installation

Grab the latest release from the [Releases](https://github.com/loreggia/micser/releases) page.

Download either `Micser-win-Setup.exe` for the installer (installs for the current user) or `Micser-win-Portable.zip` for the portable version.

## Building

### Requirements

- .NET SDK 10.0.100 or later (see `global.json`)
- Node.js 24 or later (npm enforces it); with [Volta](https://volta.sh), the pinned version in `package.json` is used
- WebView2 Runtime (included in Windows 11)

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
./scripts/pack.ps1 -Version 0.1.0   # Velopack setup and update packages in artifacts/releases
```

Pushing a tag `vX.Y.Z` builds the release on GitHub Actions and publishes it as a GitHub release, which installed copies update from.

### Driver

The virtual audio cable driver (`src/Vac`) is not part of `Micser.slnx`. It needs Visual Studio with the Windows Driver Kit component and the Spectre-mitigated libraries; the WDK itself comes from NuGet.

```powershell
./scripts/build-vac.ps1 -Platform x64,ARM64   # test-signed driver packages in src/Vac/bin
./scripts/codeql-vac.ps1                      # Microsoft's CodeQL driver checks
```

## Plugins

Audio modules are provided by plugins. Each plugin consists of a .NET project and a web package with the widgets for its modules. The main modules (device input/output, gain, compressor, ...) are provided by `src/Plugins/Main`, which also serves as an example of how to implement a plugin.

## Architecture

The architecture is described in [docs/Architecture.md](docs/Architecture.md).

The previous WPF version is commit `9386ea4` in the history of `main`.

## Credits

This project uses the following libraries and tools:

Engine and shell

- [NAudio](https://github.com/naudio/NAudio) (audio I/O and resampling)
- [ASP.NET Core](https://github.com/dotnet/aspnetcore) and [SignalR](https://learn.microsoft.com/aspnet/core/signalr/) (engine API)
- [Serilog](https://serilog.net/) (logging)
- [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet) (telemetry in development)
- [WebView2](https://learn.microsoft.com/microsoft-edge/webview2/) (shell window)
- [Velopack](https://velopack.io) (installer and updates)

Web UI

- [React](https://react.dev/)
- [Fluent UI React](https://react.fluentui.dev/)
- [React Flow](https://reactflow.dev/) (`@xyflow/react`, routing graph)
- [TanStack Query](https://tanstack.com/query)
- [i18next](https://www.i18next.com/) and [react-i18next](https://react.i18next.com/)
- [markdown-to-jsx](https://github.com/quantizor/markdown-to-jsx) (release notes)
- [Vite](https://vite.dev/)
- [Orval](https://orval.dev/) (API client generation)

Driver

- [Windows Driver Samples](https://github.com/Microsoft/Windows-driver-samples) (the VAC driver is based on SimpleAudioSample)
- [Windows Driver Kit](https://learn.microsoft.com/windows-hardware/drivers/) and [CodeQL](https://codeql.github.com/) (driver build and checks)

Development and tests

- [Aspire](https://aspire.dev) (development orchestration)
- [TUnit](https://github.com/thomhurst/TUnit) and [NSubstitute](https://nsubstitute.github.io/) (.NET tests)
- [Vitest](https://vitest.dev/) and [Playwright](https://playwright.dev/) (web and end-to-end tests)
- [TypeScript](https://www.typescriptlang.org/), [ESLint](https://eslint.org/) and [Prettier](https://prettier.io/)
- [CSharpier](https://csharpier.com/) (C# and MSBuild formatting) and [Husky.Net](https://alirezanet.github.io/Husky.Net/) (pre-commit hook)
- [LavaMoat allow-scripts](https://github.com/LavaMoat/LavaMoat/tree/main/packages/allow-scripts) (npm install script allowlist)
