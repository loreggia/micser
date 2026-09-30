# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project state

Micser (a Windows audio router) is being rebuilt on the `modernization` branch. It started from `9386ea4` on `master`, the last compiling commit of the old WPF version. `docs/Architecture.md` holds the decisions (process model, UI, IPC, storage, audio model, layout, libraries) and the roadmap; read it before structural changes and keep it updated when a decision changes. The `master`, `dev`, `naudio` and `dev-temp` branches are reference material only. Porting code from them means rewriting it to the new architecture, not merging.

## Commands

```sh
dotnet build Micser.slnx
dotnet test --solution Micser.slnx                      # TUnit on Microsoft.Testing.Platform (global.json "test.runner")
dotnet test --project tests/Engine/Micser.Engine.Tests.csproj --treenode-filter "/*/*/HealthEndpointTests/*"

npm install                  # root; installs all workspaces
npm run build                # typecheck + vite build of src/Web
npm run typecheck            # tsc in every workspace
npm run lint                 # eslint (flat config at the root)
npm run format:check         # prettier; .prettierignore limits it to the web workspaces

dotnet run --project src/Engine     # http://127.0.0.1:5080
npm run dev                         # Vite on http://localhost:5173, proxies /api and /hubs to the engine
dotnet run --project src/Shell      # tray + WebView2; optional URL argument, default is the Vite dev server
```

CI (`.github/workflows/ci.yml`) runs the dotnet build/test and npm format/lint/build as separate jobs.

## Layout and conventions

- `src/` holds everything that ships, grouped by area with short folder names, e.g. `src/Audio/Micser.Audio.csproj`. `tests/` mirrors `src/`, e.g. `tests/Engine/Micser.Engine.Tests.csproj`.
- A plugin is one folder containing both halves: `src/Plugins/Main/Micser.Plugins.Main.csproj` plus its widget package `src/Plugins/Main/Web` (`@micser/plugin-main`). `Directory.Build.props` excludes `Web/**` and `node_modules/**` from .NET item globs.
- npm workspaces (root `package.json`): `src/Web` (Vite SPA), `src/WebSdk` (widget contract and shared code), `src/Plugins/*/Web`. The internal packages export TypeScript source (`"exports": "./src/index.ts"`) and have no build step.
- Widgets are matched to engine modules by module type name. Connector names come from the engine's module definitions, never hard-coded in widgets.
- Dependency direction:
  - .NET: `Plugins → Audio` and `Engine → Audio, Plugins`. `Shell` references no Micser project and talks to the engine over HTTP only.
  - npm: `plugin-* → web-sdk` and `web → web-sdk, plugin-*`.
- Package versions are central in `Directory.Packages.props`, so `PackageReference` items carry no `Version`. `TreatWarningsAsErrors` is on for all projects.
- Libraries:
  - Serilog via `Microsoft.Extensions.Logging` (configured from `appsettings*.json`; the file sink is only enabled in Production).
  - `System.Text.Json`, and TUnit + NSubstitute for tests.
  - NAudio for audio I/O. There's no Newtonsoft, EF Core, Prism/Unity or xUnit.
- Prettier style: 4 spaces, double quotes, semicolons, print width 120.
- `src/Driver` (C++ WDM driver), `src/Installer` (WixSharp, .NET Framework) and `src/DriverUtility` (still on CSCore) were moved unchanged and aren't in `Micser.slnx`.
