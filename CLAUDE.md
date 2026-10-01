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
npm run generate:api -w @micser/web-sdk   # regenerate the API client after engine API changes (build the engine first)

aspire start                        # AppHost (tools/AppHost): engine + Vite, shell on demand, dashboard; "aspire stop" ends it
dotnet run --project src/Engine     # http://127.0.0.1:5080
npm run dev                         # Vite on http://localhost:5173, proxies /api and /hubs to the engine
dotnet run --project src/Shell -- --ui http://localhost:5173   # tray + WebView2 showing Vite; uses the running dev engine

dotnet run --project tools/AudioHarness -- list                    # audio devices
dotnet run --project tools/AudioHarness -- 1 2 --gain -100         # input 1 -> gain -> output 2, prints buffer stats
dotnet run --project tools/AudioHarness -- latency 2               # round-trip latency of output 2 via loopback (plays -40 dB noise bursts)
```

The harness opens real devices: use a very low gain (as above) unless audible output is intended.

CI (`.github/workflows/ci.yml`) runs the dotnet build/test and npm format/lint/build as separate jobs.

## Working preferences

- Don't commit unless explicitly told to. Leave changes uncommitted so the user can review them first.
- Keep code comments short and concise. Describe how the code is, not how it was or why it changed. The exception is when leaving out the history would set a trap for future changes. Comments about concrete future plans are fine.

## Layout and conventions

- `src/` holds everything that ships, grouped by area with short folder names, e.g. `src/Audio/Micser.Audio.csproj`. `tests/` mirrors `src/`, e.g. `tests/Engine/Micser.Engine.Tests.csproj`. `tools/` holds dev-only programs.
- A plugin is one folder containing both halves: `src/Plugins/Main/Micser.Plugins.Main.csproj` plus its widget package `src/Plugins/Main/Web` (`@micser/plugin-main`). `Directory.Build.props` excludes `Web/**` and `node_modules/**` from .NET item globs.
- npm workspaces (root `package.json`): `src/Web` (Vite SPA), `src/WebSdk` (widget contract and shared code), `src/Plugins/*/Web`. The internal packages export TypeScript source (`"exports": "./src/index.ts"`) and have no build step.
- Widgets are matched to engine modules by module type name. Connector names come from the engine's module definitions, never hard-coded in widgets.
- Web UI (see "UI" in `docs/Architecture.md`):
  - Engine API changes flow through `src/WebSdk/openapi/engine.json` (written on engine build) and the generated Orval client in `src/WebSdk/src/api/generated`. Never edit them by hand, and commit both.
  - Engine data comes from the generated query hooks; `EngineConnection` keeps the cache in sync via SignalR, so don't poll or invalidate after mutations. Module edits go through `useModuleUpdate`.
  - Fluent UI v9 components and `makeStyles` with `tokens`; no hard-coded colors.
  - SignalR `hub.on` handlers must not return a value; SignalR would send it to the server as an invocation result.
- Dependency direction:
  - .NET: `Plugins → Audio` and `Engine → Audio, Plugins, ServiceDefaults`. `tools/AppHost` references the runnable projects. `Shell` references no Micser project and talks to the engine over HTTP only.
  - npm: `plugin-* → web-sdk` and `web → web-sdk, plugin-*`.
- Package versions are central in `Directory.Packages.props`, so `PackageReference` items carry no `Version`. `TreatWarningsAsErrors` is on for all projects.
- Libraries:
  - Serilog via `Microsoft.Extensions.Logging` (configured from `appsettings*.json`; the file sink is only enabled in Production).
  - `System.Text.Json`, and TUnit + NSubstitute for tests.
  - NAudio for audio I/O. There's no Newtonsoft, EF Core, Prism/Unity or xUnit.
- Prettier style: 2 spaces, double quotes, semicolons, print width 120.
- C# code is cleaned up with CodeMaid (settings in `CodeMaid.config`). Write new code in its layout so a cleanup run doesn't reshuffle it:
  - Member order by type: fields, constructors, destructors, delegates, events, properties, indexers, methods, nested enums, interfaces, structs, classes.
  - Within a type group: by access level (public, internal, protected, private), then alphabetically.
  - A blank line before and after single-line properties.
  - Comments wrap at 150 columns.
- No primary constructors on classes or structs: use explicit constructors that assign `_camelCase` fields or properties. Positional records are fine.
- Audio code (`src/Audio`, see "Audio engine" in `docs/Architecture.md`):
  - `Process` methods run on the audio thread. Don't allocate, lock (except the existing per-block locks) or log there on the normal path.
  - Module parameters are plain properties written from other threads. A parameter set that must change atomically is replaced as a whole (see `EqualizerModule.Bands`).
  - Tests drive `AudioGraph.Process()` directly with synthetic modules. Nothing in `tests/` opens real devices.
- Engine code (`src/Engine`, see "Engine" in `docs/Architecture.md`):
  - A new module type needs its own state record (data annotations on the record's parameters, as in ASP.NET Core), `IStatefulModule<TState>`, and a registration in its plugin's `Add…Plugin()`. The engine, API, config file and OpenAPI pick it up from there.
  - `AudioHost` is the single entry point for graph changes. It persists and broadcasts every change, and throws `EngineRequestException` for problem responses.
  - Engine tests use `EngineFactory` (temp config directory, token required) with a real audio engine and no devices selected.
- `src/Driver` (C++ WDM driver) and `src/Installer` (WixSharp, .NET Framework) were moved unchanged and aren't in `Micser.slnx`.
