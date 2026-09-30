# Architecture

Target architecture for the modernization of Micser (decided 2026-09-30). The `modernization` branch starts from `9386ea4`, the last compiling commit on `master` (WPF + Prism app, Windows-service engine, named-pipe IPC, .NET Core 3.1). The `dev` branch (an abandoned web-UI refactor, see `docs/Inventory.md` on `dev`) and the `naudio`/`dev-temp` branches are references only. None of them is merged.

## Decisions

| # | Topic | Decision | Replaces |
|---|---|---|---|
| 1 | Process model | **Per-user headless engine process**, started at login. The UI can be closed while audio keeps running. | Windows service (session 0) |
| 2 | UI | **Vite + React + TypeScript SPA** with `@xyflow/react` for the routing graph, hosted in a **thin .NET desktop shell** (tray icon + WebView2 window). | WPF + vendored Prism + Unity |
| 3 | UI ↔ engine | **ASP.NET Core minimal APIs + SignalR on loopback** (127.0.0.1, random port, per-session token). OpenAPI spec → generated TS client. | Named pipes + MessagePack |
| 4 | Storage | **One versioned JSON config file** (System.Text.Json) under `%AppData%\Micser`: modules, connections, module state, settings. No backwards compatibility with old data. | EF Core + SQLite (in both processes) |
| 5 | Plugins | **Built-in modules behind a plugin-ready API.** Each plugin is one folder holding its .NET project and its widget package (`Web/`). The engine references plugins at compile time, and their widgets are bundled into the SPA. No runtime DLL/JS loading yet. | Runtime-scanned WPF-dependent plugin assemblies |
| 6 | Audio model | **Block-based graph with one engine clock**: float32 buffers of N frames at a fixed engine sample rate. Device inputs are buffered and resampled into the engine format to absorb clock drift, and outputs pull from the graph. Processors work on `Span<float>`, with no per-sample virtual calls or allocations. Audio I/O uses **NAudio** (WASAPI). | Per-sample push via CSCore |
| 7 | Layout | **`src/` = everything that ships, grouped by component** with short area names (the PowerToys/aspnetcore style; see below), plus `tests/` mirroring `src/`. One `Micser.slnx`, repo-wide `Directory.Build.props`, `Directory.Packages.props` and `global.json`, and a root `package.json` with npm workspaces replacing yalc. | Everything under `src/` |
| 8 | Packaging | **Deferred.** `src/Installer/` is kept as it is and not built. The custom update check is dropped for now. Decide between Velopack and WiX 5/6 once the app runs end to end. | WixSharp/WiX 3 + custom `HttpUpdateService` |

### Defaults (not discussed separately; change if you disagree)

- **.NET:** .NET 10, `LangVersion latest`, nullable enabled, central package management.
- **Hosting and libraries:**
  - `Microsoft.Extensions.Hosting` and the built-in DI. Unity, Prism and CommonServiceLocator are dropped.
  - `System.Text.Json` everywhere. Newtonsoft.Json and MessagePack are dropped.
  - Logging goes through `Microsoft.Extensions.Logging`, with Serilog as the provider (`Serilog`, `Serilog.Extensions.Logging`). NLog is dropped.
- **Tests:** TUnit and NSubstitute. xUnit and Moq are dropped.
- **Engine host:** `Microsoft.NET.Sdk.Web` (Kestrel). It serves the built SPA as static files. In development, Vite runs separately and proxies `/api` and `/hubs` to the engine.
- **Engine discovery and security:**
  - On start, the engine writes `{ port, token }` to `%LocalAppData%\Micser\engine.json` (user-only ACL).
  - The shell reads that file and passes the token to the SPA. The API rejects requests without it.
  - A per-session named mutex keeps it to one engine per user.
- **Autostart:** an `HKCU\...\Run` entry for the shell. The shell launches the engine if it isn't running.
- **Deferred to the UI phase:** the SPA's component library (antd v5 or alternatives) and its state management.

## Layout

The top level separates product code, tests, build tooling and docs, not languages. `src/` holds everything that ships, grouped by component under short area names; one area can mix stacks. Folders use short names, and the .NET projects inside them keep their full names (`src/Audio/Micser.Audio.csproj`).

```
Micser.slnx
global.json
Directory.Build.props         also excludes */Web/** and node_modules from .NET item globs
Directory.Packages.props
package.json                  npm workspaces: src/Web, src/WebSdk, src/Plugins/*/Web
src/
  Audio/                      Micser.Audio: graph, module/processor abstractions, NAudio device I/O
  Engine/                     Micser.Engine: host, HTTP API, SignalR hubs, config store, lifecycle
  Plugins/
    Main/                     Micser.Plugins.Main: built-in modules
      Modules/                  device in/out, loopback, gain, compressor, EQ, pitch, spectrum
      Processors/
      Web/                      @micser/plugin-main: widgets for these modules
  Web/                        @micser/web: Vite + React SPA (graph editor, pages)
  WebSdk/                     @micser/web-sdk: widget contract, shared controls, API client, types
  Shell/                      Micser.Shell: tray + WebView2 window, engine launcher; no app logic
  DriverUtility/              Micser.DriverUtility: VAC driver install/config CLI
  Driver/                     C++ VAC driver (moved as is, not built by default)
  Installer/                  moved as is, not built (see decision 8)
tests/                        mirrors src/
  Audio/                      Micser.Audio.Tests
  Engine/                     Micser.Engine.Tests
  Plugins/Main/               Micser.Plugins.Main.Tests
eng/                          CI and build scripts (when needed)
docs/
```

**Plugin layout.** A plugin is one folder containing both halves: the .NET project and its `Web/` npm package. Adding or changing a module touches one folder, and the folder is already the unit that runtime-loaded plugins will ship as later. Widget tests sit next to the widgets (`*.test.tsx`).

**Module contract.** The engine exposes module definitions (type name, input/output connectors, state schema) through the API. Widgets are registered by module type name and read connectors from the definition instead of hard-coding them. This avoids the name drift seen on `dev` (`Output` vs. `Output01`).

**Dependencies** go one way:
- .NET: `Plugins.Main → Audio`, `Engine → Audio, Plugins.Main`, and `Shell → nothing` (it talks to the engine only over HTTP).
- npm: `plugin-main → web-sdk`, and `web → web-sdk, plugin-main`.

## Roadmap

1. **Skeleton:**
   - Create the new layout on .NET 10 with the props files, the npm workspace root, and a CI workflow (`dotnet build`/`test` plus the npm build and lint).
   - Remove the old WPF/Prism/engine projects. They stay available in git history and on `master`.
   - Move the driver and installer to `src/Driver` and `src/Installer` as they are.
2. **Audio (`src/Audio`):**
   - Graph, block processing, format and resampling, and NAudio device enumeration, capture and render.
   - Port the DSP code (gain, compressor, EQ, pitch, spectrum) from `master`'s `Micser.Plugins.Main` into `src/Plugins/Main`, using the `naudio`/`dev-temp` branches for the API mapping.
   - Unit tests, plus a small console harness that routes input → gain → output as a smoke test.
3. **Engine (`src/Engine`):** hosting, the JSON config store, module definition/module/connection/device/settings APIs, and SignalR hubs for level, spectrum and device-change pushes.
4. **UI:** `src/Web`, `src/WebSdk` and `src/Plugins/Main/Web`: the Vite app, graph editor and widgets. `dev`'s `Micser/UI` Dashboard serves as a guideline.
5. **Shell (`src/Shell`):** tray, WebView2 window, engine launch and discovery, autostart.
6. **Later:** packaging and updates, the VAC driver (needs an EV code-signing cert), and runtime-loaded plugins.
