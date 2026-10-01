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
| 6 | Audio model | **Block-based graph with one engine clock and per-connection channel layouts** (see [Audio engine](#audio-engine)). Audio I/O uses **NAudio** (WASAPI). | Per-sample push via CSCore |
| 7 | Layout | **`src/` = everything that ships, grouped by component** with short area names (the PowerToys/aspnetcore style; see below), plus `tests/` mirroring `src/` and `tools/` for dev-only programs. One `Micser.slnx`, repo-wide `Directory.Build.props`, `Directory.Packages.props` and `global.json`, and a root `package.json` with npm workspaces replacing yalc. | Everything under `src/` |
| 8 | Packaging | **Deferred.** `src/Installer/` is kept as it is and not built. The custom update check is dropped for now. Decide between Velopack and WiX 5/6 once the app runs end to end. | WixSharp/WiX 3 + custom `HttpUpdateService` |

### Defaults (not discussed separately; change if you disagree)

- **.NET:** .NET 10, `LangVersion latest`, nullable enabled, central package management.
- **Hosting and libraries:**
  - `Microsoft.Extensions.Hosting` and the built-in DI. Unity, Prism and CommonServiceLocator are dropped.
  - `System.Text.Json` everywhere. Newtonsoft.Json and MessagePack are dropped.
  - Logging goes through `Microsoft.Extensions.Logging`, with Serilog as the provider (`Serilog`, `Serilog.Extensions.Logging`). NLog is dropped.
- **Tests:** TUnit and NSubstitute. xUnit and Moq are dropped.
- **Engine host:** `Microsoft.NET.Sdk.Web` (Kestrel). It serves the built SPA as static files (roadmap step 4). In development, Vite runs separately and proxies `/api` and `/hubs` to the engine, which then listens on the fixed address `http://127.0.0.1:5080` without requiring the token. `AllowedHosts` is limited to `localhost;127.0.0.1` against DNS rebinding.
- **Shell:** WinForms (native `NotifyIcon`) with the WebView2 WinForms control. It has no app logic, so WPF isn't needed.
- **Web tooling:** npm workspaces consume the internal packages (`@micser/web-sdk`, `@micser/plugin-main`) as TypeScript source, so they need no build step of their own. TypeScript is pinned to `~6.0` because `typescript-eslint` doesn't support 7.x yet.
- **Engine discovery and security:** see [Engine](#engine).
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
      Dsp/                      DSP helpers not covered by NAudio
      Web/                      @micser/plugin-main: widgets for these modules
  Web/                        @micser/web: Vite + React SPA (graph editor, pages)
  WebSdk/                     @micser/web-sdk: widget contract, shared controls, API client, types
  Shell/                      Micser.Shell: tray + WebView2 window, engine launcher; no app logic
  DriverUtility/              Micser.DriverUtility: VAC driver install/config CLI (standalone)
  Driver/                     C++ VAC driver (moved as is, not built by default)
  Installer/                  moved as is, not built (see decision 8)
tests/                        mirrors src/
  Audio/                      Micser.Audio.Tests
  Engine/                     Micser.Engine.Tests
  Plugins/Main/               Micser.Plugins.Main.Tests
tools/                        dev-only programs, in Micser.slnx but never shipped
  AudioHarness/               routes a real input through a gain module to a real output and prints buffer statistics
eng/                          CI and build scripts (when needed)
docs/
```

**Plugin layout.** A plugin is one folder containing both halves: the .NET project and its `Web/` npm package. Adding or changing a module touches one folder, and the folder is already the unit that runtime-loaded plugins will ship as later. Widget tests sit next to the widgets (`*.test.tsx`).

**Module contract.** The engine exposes module definitions (type name, input/output connectors, state schema) through the API. Widgets are registered by module type name and read connectors from the definition instead of hard-coding them. This avoids the name drift seen on `dev` (`Output` vs. `Output01`).

**Dependencies** go one way:
- .NET: `Plugins.Main → Audio`, `Engine → Audio, Plugins.Main`, and `Shell → nothing` (it talks to the engine only over HTTP). `DriverUtility` is standalone.
- npm: `plugin-main → web-sdk`, and `web → web-sdk, plugin-main`.

## Audio engine

- **Blocks and clock.** `AudioEngine` processes the `AudioGraph` on an MMCSS "Pro Audio" thread, one block per block duration (default 48 kHz, 240 frames = 5 ms), paced by a high-resolution waitable timer against absolute deadlines. If it falls more than 4 blocks behind, it skips ahead instead of catching up.
- **Graph.** Modules have named input and output ports. Each block, modules run in dependency order; every input port first receives the sum of its connected outputs. Cycles are rejected. Edits swap in a new processing plan under a short lock that the audio thread holds per block, so a module can be disposed as soon as `Remove` returns. A module that throws produces silence for that block.
- **Buffers and layouts.** Buffers are planar float32 and carry a `ChannelLayout` (channel count + WAVEFORMATEXTENSIBLE speaker mask). Layouts can differ per connection. An input port either has a fixed layout (e.g. a device output uses the device's layout) or takes the widest layout of its sources. `ChannelMixer` converts between layouts:
  - mono sources go to front center, or else to front left and right;
  - mono targets get the average of all channels except LFE;
  - positional layouts map matching speakers 1:1 and fold missing ones into their neighbours (center into L/R at -3 dB, sides and backs into each other or into the fronts at -3 dB), dropping LFE;
  - everything else maps by index.
- **Volume.** Every module has `Volume` (0..1) and `IsMuted`, applied to its outputs with a ramp over one block. `EffectModule` adds `IsBypassed`. Samples aren't clamped inside the graph, only at device outputs.
- **Devices.** Each capture and render stream decouples its device clock from the engine clock:
  - A lock-free single-producer/single-consumer ring buffer sits between them. Its target fill is one device period (devices deliver and consume whole periods, 10 ms in shared mode) plus half an engine block.
  - The WASAPI render buffer is requested at 20 ms; NAudio's default of 200 ms dominated the latency before.
  - A windowed-sinc resampler (NAudio's `WdlResampler`) converts between the device and engine rates.
  - A PI controller (`DriftController`) adjusts the resampling ratio by up to ±0.5% to hold the target fill.
  - When the fill is far off (e.g. after a pause or at startup), the stream resynchronizes by discarding samples or inserting silence.
- **Device modules** follow their device's state and, when the device ID disappears, switch to another active device of the same adapter (e.g. a USB device plugged into a different port).

## Engine

- **Plugin API.**
  - Plugins register module types with `services.AddAudioModule<TModule, TState>("Type")`.
  - Every module implements `IStatefulModule<TState>`. `TState` is an immutable record with data annotations, and each module type has its own.
  - Modules raise `StateChanged` when they change their own state (e.g. a device module switching ports), so the engine persists and broadcasts it.
  - Modules with live data (spectrum, device stream statistics) implement `IModuleDataSource`.
- **`ModuleDto`** is polymorphic by `type`: `ModuleDto<TState>` per module type, registered at runtime (`ModuleCatalog`, `EngineJson`).
  - It carries the id, name, UI position, volume, mute, bypass (effects only) and the typed `state`.
  - The API, SignalR and the config file all use the same schema. OpenAPI shows it as `anyOf` with a discriminator mapping, so a generated TS client narrows `state` by `type`.
- **API** (`/api`, see `src/Engine/Endpoints/ApiEndpoints.cs`):
  - `health`, `module-types` (ports and default state), `modules` (create with defaults, full update with `PUT`, delete), `connections`, `devices`, and `engine` (status, start, stop, settings).
  - Errors are problem details: 400 with `errors` keyed by camelCase property path (e.g. `state.bands[1].frequency`), 404, and 409 for cycles and duplicates.
- **Hub** (`/hubs/engine`):
  - Pushes `ModuleChanged`, `ModuleRemoved`, `ConnectionAdded`, `ConnectionRemoved`, `DevicesChanged` and `StatusChanged` to all clients, in the order they happened.
  - `Subscribe(moduleId)` / `Unsubscribe(moduleId)` start and stop `ModuleData` pushes (20 per second) for modules with live data.
- **Configuration** (`%AppData%\Micser\config.json`, `Engine:ConfigPath`):
  - It's versioned, with saves debounced (500 ms) and written atomically.
  - An unreadable file is moved to `config.json.<timestamp>.bak`, and the engine starts empty.
  - Modules of unknown type, invalid modules and dangling connections are skipped when loading.
  - Changing the engine settings rebuilds the graph.
- **Discovery and security:**
  - The engine binds to `127.0.0.1` with a random port and writes `{ url, token, processId }` to `%LocalAppData%\Micser\engine.json` (`Engine:DiscoveryPath`). That folder is private to the user. The file is deleted on a clean shutdown; after a crash it stays, so readers must check that the process is alive.
  - `/api` and `/hubs` (except `/api/health`) require `Authorization: Bearer <token>`, or `access_token` in the query for SignalR from browsers.
  - A named semaphore (`Local\Micser.Engine`) allows one engine per session. `Engine:RequireToken` and `Engine:SingleInstance` turn these off (development, tests).

## Roadmap

1. **Skeleton** (done):
   - Create the new layout on .NET 10 with the props files, the npm workspace root, and a CI workflow (`dotnet build`/`test` plus the npm build and lint).
   - Remove the old WPF/Prism/engine projects. They stay available in git history and on `master`.
   - Move the driver and installer to `src/Driver` and `src/Installer` as they are.
   - Move `Micser.DriverUtility` to `src/DriverUtility` as it is. It still references the removed `Micser.Common` and CSCore, so it stays outside the solution until step 2 ports it to NAudio.
2. **Audio (`src/Audio`)** (done):
   - Graph, block processing, format and resampling, and NAudio device enumeration, capture and render.
   - Port the DSP code (gain, compressor, EQ, pitch, spectrum) from `master`'s `Micser.Plugins.Main` into `src/Plugins/Main`, using the `naudio`/`dev-temp` branches for the API mapping.
   - Port `src/DriverUtility` and add it to the solution.
   - Unit tests, plus a small console harness that routes input → gain → output as a smoke test (`tools/AudioHarness`). Its `latency` mode measures the software round trip (render + loopback capture) by cross-correlating a quiet noise burst: about 47–50 ms on a 48 kHz USB interface, from 240 ms with NAudio's default render buffer and 10 ms blocks.
   - Follow-ups:
     - The drift controller measures the ring fill once per block. The reading depends on the phase between device periods and engine blocks, so after startup it can take ~10 s (at up to ~1500 ppm) to settle. Measuring against the WASAPI QPC timestamps would avoid the phase bias.
     - `master`'s "use system volume" option (following the Windows master volume) isn't ported yet.
     - Lower latency needs device periods below 10 ms. `IAudioClient3` low-latency mode (NAudio `WithLowLatency`) wasn't available on the tested devices and made loopback capture fail; exclusive mode would work but takes the device away from other applications.
3. **Engine (`src/Engine`)** (done): hosting, the JSON config store, module definition/module/connection/device/settings APIs, and SignalR hubs for change and module data pushes.
   - Follow-ups:
     - Level meters per module (a module data source in the audio core).
     - Device streams that fault (e.g. after sleep) are only reopened on device events; a periodic health check could reopen them.
4. **UI:** `src/Web`, `src/WebSdk` and `src/Plugins/Main/Web`: the Vite app, graph editor and widgets. `dev`'s `Micser/UI` Dashboard serves as a guideline.
5. **Shell (`src/Shell`):** tray, WebView2 window, engine launch and discovery, autostart.
6. **Later:** packaging and updates, the VAC driver (needs an EV code-signing cert), and runtime-loaded plugins.
