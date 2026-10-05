# Architecture

Target architecture for the modernization of Micser (decided 2026-09-30). `main` was rebuilt starting from `9386ea4`, the last compiling commit of the old version (WPF + Prism app, Windows-service engine, named-pipe IPC, .NET Core 3.1). The `dev` branch (an abandoned web-UI refactor, see `docs/Inventory.md` on `dev`) and the `naudio`/`dev-temp` branches are references only. None of them is merged.

## Decisions

| # | Topic | Decision | Replaces |
|---|---|---|---|
| 1 | Process model | **Per-user headless engine process**, started at login. The UI can be closed while audio keeps running. | Windows service (session 0) |
| 2 | UI | **Vite + React + TypeScript SPA** with `@xyflow/react` for the routing graph, hosted in a **thin .NET desktop shell** (tray icon + WebView2 window). | WPF + vendored Prism + Unity |
| 3 | UI ↔ engine | **ASP.NET Core minimal APIs + SignalR on loopback** (127.0.0.1, random port, per-session token). OpenAPI spec → generated TS client. | Named pipes + MessagePack |
| 4 | Storage | **One versioned JSON config file** (System.Text.Json) under `%AppData%\Micser`: modules, connections, module state, settings. No backwards compatibility with old data. | EF Core + SQLite (in both processes) |
| 5 | Plugins | **Runtime-loaded plugins** (see [Plugins](#plugins)). Each plugin is one folder holding its .NET project and its widget package (`Web/`). The engine loads plugin assemblies from a plugins folder at startup, each in its own `AssemblyLoadContext`, and the SPA imports their widget bundles (ES modules) from the engine. The built-in Main plugin is loaded the same way. Plugins are installed and removed from the UI and take effect when the engine restarts. | Runtime-scanned WPF-dependent plugin assemblies |
| 6 | Audio model | **Block-based graph with one engine clock and per-connection channel layouts** (see [Audio engine](#audio-engine)). Audio I/O uses **NAudio** (WASAPI). | Per-sample push via CSCore |
| 7 | Layout | **`src/` = everything that ships, grouped by component** with short area names (the PowerToys/aspnetcore style; see below), plus `tests/` mirroring `src/` and `tools/` for dev-only programs. One `Micser.slnx`, repo-wide `Directory.Build.props`, `Directory.Packages.props` and `global.json`, and a root `package.json` with npm workspaces replacing yalc. | Everything under `src/` |
| 8 | Packaging | **Velopack** (see [Packaging and updates](#packaging-and-updates)): a per-user install without admin rights, with delta updates from GitHub releases. The old WixSharp installer was removed; it's in git history. | WixSharp/WiX 3 + custom `HttpUpdateService` |

### Defaults (not discussed separately; change if you disagree)

- **.NET:** .NET 10, `LangVersion latest`, nullable enabled, central package management.
- **Hosting and libraries:**
  - `Microsoft.Extensions.Hosting` and the built-in DI. Unity, Prism and CommonServiceLocator are dropped.
  - `System.Text.Json` everywhere. Newtonsoft.Json and MessagePack are dropped.
  - Logging goes through `Microsoft.Extensions.Logging`, with Serilog as the provider (`Serilog`, `Serilog.Extensions.Logging`). NLog is dropped.
- **Tests:** TUnit and NSubstitute. xUnit and Moq are dropped.
- **Web tests:** Vitest, from one root `vitest.config.ts` with two projects. `*.test.ts` runs in Node. `*.test.tsx` and `*.browser.test.ts` (DOM, storage, module imports) run in Vitest browser mode on headless Chromium through Playwright, because Fluent UI and React Flow need real layout. Components are rendered with `vitest-browser-react`. `@micser/web-sdk/testing` has the shared helpers: `TestProviders` (theme, query client, an engine connection that is never started), `createTestQueryClient()` (seeded with `setQueryData` under the generated keys, so hooks don't fetch) and `testModule()`.
- **End-to-end tests:** Playwright Test in `tests/E2E` (npm workspace `@micser/e2e`), `npm run test:e2e`.
  - `globalSetup.ts` builds the engine once (Release in CI), and a plugin package: the engine tests' plugin (module type "Test") with the hand-written widget bundle in `tests/E2E/plugin/web`, which imports `react` through the UI's import map like a real plugin bundle.
  - Each worker starts its own engine from the build output and its own Vite dev server, both on free ports (`servers.ts`, the worker-scoped `servers` fixture), so the tests run in parallel: 35 tests took about 50 s with 8 workers, against 3.6 min on one shared engine.
    - The engine listens on port 0 and is ready once it has written its discovery file, which has the port. It gets a temporary config and user plugin folder, with no token and no single-instance check.
    - Vite runs through its API with its own dependency cache per worker; concurrent servers would otherwise write the same one. The fixture loads the UI once before the worker's first test, since the first load compiles it, slowly while the other workers do the same.
  - A worker's tests share its engine, and each starts from an empty graph: the `engine` fixture deletes subgraphs, modules and templates and restores the default preferences.
    - Tests that change more, e.g. the plugins, get an engine and Vite server of their own (`test.use({ ownEngine: true })`). The `app` fixture's `restartEngine()` stops that engine gracefully (`POST /api/engine/shutdown`) and starts it at the same address, as the shell does.
  - `EngineApi` sets up state and checks results through the HTTP API; `Graph` wraps the React Flow DOM (nodes by `data-id`, ports, connections, menus, notifications).
  - `FakeShell` injects `chrome.webview` before the UI loads: it answers `getState` with a given `ShellState`, records the messages the UI posts and sends the shell's messages, so the shell-only controls are tested without WebView2.
  - Chromium only: the shell's WebView2 is Chromium as well.
- **Engine host:** `Microsoft.NET.Sdk.Web` (Kestrel). It serves the built SPA as static files (roadmap step 4). In development, Vite runs separately and proxies `/api` and `/hubs` to the engine, which then listens on the fixed address `http://127.0.0.1:5080` without requiring the token. `AllowedHosts` is limited to `localhost;127.0.0.1` against DNS rebinding.
- **Shell:** WinForms (native `NotifyIcon`) with the WebView2 WinForms control. It has no app logic, so WPF isn't needed.
- **Web tooling:** npm workspaces consume the internal packages (`@micser/web-sdk`, `@micser/plugin-main`) as TypeScript source. Only the SPA and the plugins' widget bundles are built (`npm run build` builds every workspace with a `build` script). TypeScript is pinned to `~6.0` because `typescript-eslint` doesn't support 7.x yet. Volta pins the Node version (`volta.node` in `package.json`), and npm refuses to install on a Node outside the `engines` ranges (`engine-strict`). npm installs run no lifecycle scripts and skip versions younger than 3 days (`.npmrc`); `@lavamoat/allow-scripts` keeps the allowlist of packages with install scripts (`lavamoat.allowScripts` in the root's and each workspace's `package.json`, since allow-scripts only follows the dependencies of the package it runs in); `npm run setup` installs and then runs the allowed ones.
- **Engine discovery and security:** see [Engine](#engine).
- **Autostart:** an `HKCU\...\Run` entry for the shell. The shell launches the engine if it isn't running (see [Shell](#shell)).
- **UI libraries:** Fluent UI React v9 (light/dark following the OS), TanStack Query for engine state, Orval for the API client, i18next with react-i18next for translations (see [UI](#ui)).

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
    Main/                     Micser.Plugins.Main: built-in modules, loaded at runtime like any plugin
      plugin.json               the plugin's manifest
      Modules/                  device in/out, loopback, gain, compressor, EQ, pitch, spectrum
      Dsp/                      DSP helpers not covered by NAudio
      Web/                      @micser/plugin-main: widgets for these modules, built to Web/dist
  Web/                        @micser/web: Vite + React SPA (graph editor, pages)
  WebSdk/                     @micser/web-sdk: widget contract, shared controls, API client, types
  Shell/                      Micser.Shell: tray + WebView2 window, engine launcher; no app logic
  ServiceDefaults/            Micser.ServiceDefaults: OpenTelemetry setup, exported only when run from the AppHost
  DriverUtility/              Micser.DriverUtility: VAC driver install/config CLI (standalone)
  Vac/                        Micser.Vac: VAC driver (PortCls WaveRT, C++), built by eng/build-vac.ps1, not in Micser.slnx
tests/                        mirrors src/
  Audio/                      Micser.Audio.Tests
  Engine/                     Micser.Engine.Tests
    TestPlugin/                 a plugin for the plugin loader tests
  Plugins/Main/               Micser.Plugins.Main.Tests
  E2E/                        @micser/e2e: Playwright end-to-end tests of the UI against a real engine
tools/                        dev-only programs, in Micser.slnx but never shipped
  AppHost/                    Micser.AppHost: Aspire AppHost that runs engine, Vite and (on demand) shell with a dashboard
  AudioHarness/               routes a real input through a gain module to a real output and prints buffer statistics; also measures latency and probes stream formats
eng/                          scripts: pack.ps1 (Velopack release), build-vac.ps1 (driver), deploy-vac-vm.ps1 (driver into a test VM)
docs/
```

**Plugin layout.** A plugin is one folder containing both halves: the .NET project (with its `plugin.json`) and its `Web/` npm package. Adding or changing a module touches one folder. Web tests sit next to the code they test (`*.test.ts`, `*.test.tsx`), widget tests next to the widgets.

**Module contract.** The engine exposes module definitions (type name, input/output connectors, state schema) through the API. Widgets are registered by module type name and read connectors from the definition instead of hard-coding them. This avoids the name drift seen on `dev` (`Output` vs. `Output01`).

**Dependencies** go one way:
- .NET: `Plugins.Main → Audio`, `Engine → Audio, ServiceDefaults`, and `Shell → nothing` (it talks to the engine only over HTTP). `DriverUtility` is standalone. The engine has a build-only reference to `Plugins.Main` (`ReferenceOutputAssembly="false"`) that copies it to `plugins/Main` without compiling against it.
- npm: `plugin-main → web-sdk`, and `web → web-sdk`. The SPA loads plugin widgets at runtime.

## Audio engine

- **Blocks and clock.** `AudioEngine` processes the `AudioGraph` on an MMCSS "Pro Audio" thread, one block per block duration (default 48 kHz, 240 frames = 5 ms), paced by a high-resolution waitable timer against absolute deadlines. If it falls more than 4 blocks behind, it skips ahead instead of catching up.
- **Graph.** Modules have named input and output ports. Each block, modules run in dependency order; every input port first receives the sum of its connected outputs. Cycles are rejected. Edits swap in a new processing plan under a short lock that the audio thread holds per block, so a module can be disposed as soon as `Remove` returns. A module that throws produces silence for that block.
- **Buffers and layouts.** Buffers are planar float32 and carry a `ChannelLayout` (channel count + WAVEFORMATEXTENSIBLE speaker mask). Layouts can differ per connection. An input port either has a fixed layout (e.g. a device output uses the device's layout) or takes the widest layout of its sources. `ChannelMixer` converts between layouts:
  - mono sources go to front center, or else to front left and right;
  - mono targets get the average of all channels except LFE;
  - positional layouts map matching speakers 1:1 and fold missing ones into their neighbours (center into L/R at -3 dB, sides and backs into each other or into the fronts at -3 dB), dropping LFE;
  - everything else maps by index.
- **Volume.** Every module has `Volume` (0..1) and `IsMuted`, applied to its outputs with a ramp over one block. `EffectModule` adds `IsBypassed`. Samples aren't clamped inside the graph, only at device outputs.
- **System volume.** A module can follow the volume and mute of Windows' default output device (`useSystemVolume`, as in the WPF version), so the volume keys control routes into devices that ignore the Windows volume (e.g. a loopback or virtual cable). `SystemVolume` watches the default device's endpoint volume and switches with the default device; `AudioHost` then sets the modules' volume (the device's level in dB as a linear gain, so loudness matches) and mute, persists and broadcasts them. The UI disables the module's volume and mute controls meanwhile.
- **Levels.** Each output port has a `LevelMeter` that measures the output after volume and mute: per channel, the peak since the last read and the RMS smoothed over 300 ms. A module without outputs measures what it passes to `ApplyVolume` (a device output: what it plays). The audio thread updates the meters with atomics only; `AudioModule.ReadLevels()` reads and resets the peaks.
- **Devices.** Each capture and render stream decouples its device clock from the engine clock:
  - A lock-free single-producer/single-consumer ring buffer sits between them. Devices deliver and consume whole periods (10 ms in shared mode), so the initial target fill is:
    - render: one device period plus half an engine block;
    - capture: a block plus half a period (a read takes a whole block while periods arrive at any phase), at least one period, plus half a block. At 5 ms blocks both are 12.5 ms; at 20 ms blocks capture needs 35 ms.
  - The target adapts per stream (`AdaptiveTarget`): each dropout raises it by half a device period (dropouts within 1 s count as one), up to 200 ms. After 10 minutes without dropouts it steps back down, but not below a level that had a dropout within 10 minutes of stepping down to it. Device widgets show the current target in ms.
  - Device modules keep the learned target in their state (`bufferMilliseconds`, saved with the configuration), so a reopened stream (engine restart, settings change, resume) starts there instead of relearning. Selecting a different device in the widget clears it. Dropouts in a stream's first second don't count, since devices may still be settling in.
  - Dropouts only count while the engine side is active: when it hasn't read or written for 100 ms (e.g. audio switched off), the device's empty reads and full writes are ignored. Render underruns are recorded on the device thread and counted by the next write, which drops them after a pause. When processing resumes, render tops up with silence and capture discards the backlog.
  - While running, render never tops up a low buffer with silence: that cut audible gaps (on a wireless headset several per minute) that the device would have ridden through. Only a real underrun counts and grows the target. A capture stream that falls more than 3 targets behind skips the excess; that counts as a dropout (`Resyncs` in the statistics) and grows the target too.
  - The WASAPI render buffer is requested at 20 ms; NAudio's default of 200 ms dominated the latency before.
  - A windowed-sinc resampler (NAudio's `WdlResampler`) converts between the device and engine rates.
  - A PI controller (`DriftController`) adjusts the resampling ratio by up to ±0.5% to hold the target fill.
  - When the fill is far off (e.g. after a pause or at startup), the stream resynchronizes by discarding samples or inserting silence.
- **Device modules** follow their device's state and, when the device ID disappears, switch to another active device of the same adapter (e.g. a USB device plugged into a different port). Setting their state reopens the stream only when the device changes, since every module update (e.g. a volume change) sets the state again.
- **Stream recovery.** A device stream is faulted when WASAPI stopped it (e.g. the device was invalidated) or when its device delivered or took no data for 2 s (10 s before the first callback, for slow devices like Bluetooth). Loopback capture is exempt from the stall check because it gets no data while nothing plays.
  - A watchdog in each device module (every second) reopens a faulted stream and retries a selected device that isn't open, with a delay of 1 s doubling up to 30 s. The delay resets after a minute of healthy streaming, on device events and on device selection.
  - `AudioDeviceService.SystemResumed` (a `PowerRegisterSuspendResumeNotification` callback, no window needed) makes all device modules reopen their streams after sleep or hibernation, since streams can look healthy then but play or capture nothing.

## Engine

- **Plugin API.**
  - A plugin assembly has exactly one public `IAudioPlugin` (in `Micser.Audio`), whose `ConfigureServices` registers module types with `services.AddAudioModule<TModule, TState>("Type")`.
  - Every module implements `IStatefulModule<TState>`. `TState` is an immutable record with data annotations, and each module type has its own.
  - Modules raise `StateChanged` when they change their own state (e.g. a device module switching ports), so the engine persists and broadcasts it.
  - Modules with live data (spectrum, device stream statistics) implement `IModuleDataSource`.
- **`ModuleDto`** is polymorphic by `type`: `ModuleDto<TState>` per module type, registered at runtime (`ModuleCatalog`, `EngineJson`).
  - It carries the id, name, UI position, subgraph, volume, mute, bypass (effects only) and the typed `state`.
  - The API, SignalR and the config file all use the same schema. OpenAPI shows it as `anyOf` with a discriminator mapping, so a generated TS client narrows `state` by `type`.
- **API** (`/api`, see `src/Engine/Endpoints/ApiEndpoints.cs`):
  - `health`, `module-types` (ports and default state), `modules` (create with defaults, full update with `PUT`, delete), `connections`, `subgraphs` and `subgraph-templates` (see below), `devices`, `engine` (status, start, stop, restart-audio, settings, shutdown), `plugins` (list, install a zip package, remove; see [Plugins](#plugins)), and `preferences`.
  - `engine/restart-audio` rebuilds the graph with the current settings, which reopens all device streams with fresh buffers.
  - `preferences` are the web UI's preferences (stream statistics, grid snapping). They live in the engine's configuration because the UI's origin (the engine's random port) changes on every start, so browser storage would lose them. Changes are pushed as `PreferencesChanged`.
  - Errors are problem details: 400 with `errors` keyed by camelCase property path (e.g. `state.bands[1].frequency`), 404, and 409 for cycles and duplicates.
- **Subgraphs** (`SubgraphDto`) group modules in the UI: name, position, size, color, collapsed, mute and bypass. A module refers to its subgraph with `subgraphId`.
  - A member's position is relative to the subgraph, so moving a subgraph is one update. Creating a subgraph from modules (`POST /api/subgraphs`, also from another subgraph) and ungrouping one (`DELETE /api/subgraphs/{id}`; its modules stay) convert the positions. With `deleteModules=true`, the delete removes the modules and their connections too. The UI converts them when it moves a module in or out with a module `PUT`.
  - The subgraph's mute and bypass combine with each member's own (`module || subgraph`, bypass for effects only). `ModuleDto` keeps the module's own flags, so turning the subgraph's off restores them.
  - There's one level; subgraphs don't nest.
- **Subgraph templates** (`SubgraphTemplateDto`) are saved subgraphs: name, color, size, modules (with template-local ids and relative positions) and the connections between them. Names are unique, ignoring case (409 otherwise).
  - Saving a subgraph (`POST /api/subgraph-templates`) creates a template or, with `templateId`, saves over one, keeping its id and increasing its `revision`. The subgraph then refers to it (`templateId`, `templateRevision`), and its modules get `templateModuleId`s, the template-local ids. Ids that modules already have are kept, so subgraphs created from an earlier revision still match.
  - Instantiating (`POST /api/subgraph-templates/{id}/instantiate`) creates a subgraph with new modules and their connections, named after the template.
  - Subgraphs are never updated automatically: one whose `templateRevision` is lower than the template's is outdated. `POST /api/subgraphs/{id}/update-from-template` matches modules by `templateModuleId` and type: matched ones keep their id and outside connections and take the template's settings and state, unmatched ones are removed, missing ones added, and the connections between the modules become the template's (unchanged ones stay). The subgraph keeps its name, position, collapse, mute and bypass, and takes the template's color and size.
  - Only the engine sets `templateId` and `templateModuleId`: a subgraph update can only clear the reference (detach), and a module's `templateModuleId` is cleared when it changes subgraphs. Removing a template clears the references of its subgraphs. Renaming is `PUT /api/subgraph-templates/{id}/name`.
  - Template modules of unknown types are kept as they are, like the graph's; such a template is listed with `unavailableTypes` and can't be instantiated or used for updates (400).
  - **Built-in templates** come with plugins (`templates` in `plugin.json`), so app and plugin updates can add and change them (`isBuiltIn`):
    - The engine loads them from the loaded plugins at every start (`LoadPluginTemplates` in `AudioHost`), before the configuration's, and doesn't save them. One that is invalid or whose id or name another plugin's template has is skipped.
    - They can't be renamed, saved over or removed (400), and their names can't be used by other templates (409). A subgraph created from one is saved as a new template; the save dialog suggests "<name> (custom)".
    - The user's templates that have a built-in template's name, e.g. one a new release added, are renamed to "<name> (custom)" (or "(custom 2)" and so on) at startup, so releases can add templates without overwriting the user's.
    - While its plugin isn't loaded, the subgraphs created from one keep their reference (see Configuration below); the UI shows "Template unavailable" and only offers detaching.
    - A plugin changes a template by increasing its `revision`. The template keeps its id and its modules' ids, which its subgraphs match when they're updated; the subgraphs then show as outdated and are updated by the user.
    - The engine tests leave them out (`Engine:LoadPluginTemplates`) except where they test them; the end-to-end `EngineApi.templates()` returns only the user's.
  - Main's templates (`src/Plugins/Main/templates.json`) are effect chains to wire between an input and an output: "Footstep boost" (EQ that cuts the lows and lifts 2.5 kHz, an upward compressor for quiet sounds, a limiter), "Night mode" (a downward compressor with make-up gain, a limiter) and "Voice chat mic" (EQ that cuts rumble and lifts presence, a compressor, an output gain).
- **Hub** (`/hubs/engine`):
  - Pushes `ModuleChanged`, `ModuleRemoved`, `ConnectionAdded`, `ConnectionRemoved`, `SubgraphChanged`, `SubgraphRemoved`, `TemplatesChanged` (the whole list), `DevicesChanged`, `PluginsChanged`, `PreferencesChanged` and `StatusChanged` to all clients, in the order they happened.
  - `Subscribe(moduleId)` / `Unsubscribe(moduleId)` start and stop `ModuleData` pushes (20 per second) for modules with live data.
  - `SubscribeLevels()` / `UnsubscribeLevels()` start and stop `Levels` pushes (20 per second): the levels of all processed modules in one message (`PortLevelsDto` per port, linear amplitude). Hub payloads aren't in the OpenAPI document, so the web SDK declares `PortLevels` itself.
- **Configuration** (`%AppData%\Micser\config.json`, `Engine:ConfigPath`):
  - It's versioned, with saves debounced (500 ms) and written atomically.
  - An unreadable file is moved to `config.json.<timestamp>.bak`, and the engine starts empty.
  - Modules of unknown type (their plugin isn't loaded) are kept in the file as they are, with their connections, but stay out of the graph and the API; they come back with their plugin. Invalid modules and dangling connections are skipped.
  - Subgraphs and templates are kept with the modules. A module whose subgraph is missing loses its `subgraphId`. A subgraph keeps the reference to a missing template, which may be a built-in one whose plugin isn't loaded, so it's linked again when the plugin is; deleting a subgraph also takes the unknown-type modules out of it.
  - Changing the engine settings rebuilds the graph.
- **Discovery and security:**
  - The engine binds to `127.0.0.1` with a random port and writes `{ url, token, processId }` to `%LocalAppData%\Micser\engine.json` (`Engine:DiscoveryPath`). That folder is private to the user. The file is deleted on a clean shutdown; after a crash it stays, so readers must check that the process is alive.
  - `/api` and `/hubs` (except `/api/health`) require `Authorization: Bearer <token>`, or `access_token` in the query for SignalR from browsers.
  - A named semaphore (`Local\Micser.Engine`) allows one engine per session. `Engine:RequireToken` and `Engine:SingleInstance` turn these off (development, tests).

## UI

- **API client:**
  - The engine build writes `src/WebSdk/openapi/engine.json` (`Microsoft.Extensions.ApiDescription.Server`).
  - `npm run generate:api -w @micser/web-sdk` generates the Orval client from it: types, fetch functions and TanStack Query hooks in `src/WebSdk/src/api/generated`.
  - Both the document and the client are committed. CI fails if either is out of date.
  - `engineFetch` adds the access token and throws `EngineApiError` with the problem details.
- **State:**
  - Engine data lives in the TanStack Query cache, which never goes stale. `EngineConnection` (SignalR) patches it from engine events and refetches everything after a reconnect.
  - Module and subgraph updates (`useModuleUpdate`, `useSubgraphUpdate`) show immediately and go to the engine debounced (80 ms, last value wins). Engine echoes are ignored while an update is pending, so controls don't jump back.
  - `useModuleData(moduleId)` subscribes to live data (spectrum, stream statistics).
- **Widgets:**
  - A plugin's `Web` package default-exports `definePlugin({ name, widgets })` with `defineWidget({ moduleType, title, component })` entries. The component receives the typed module (`WidgetProps<"Gain">`) and a `setState` function. Module types of plugins outside this repository aren't in the generated API types.
  - `PluginsProvider` imports the widget bundles of the loaded plugins (`webUrl` from `GET /api/plugins`) before the graph is shown. A bundle that fails to load shows a notification, and its modules render without a widget.
  - The graph node around it is generic: title, mute, bypass (if `supportsBypass`), collapse, a "More" menu with "Delete" (also when collapsed), volume, a level meter, and connectors from the engine's module type. Modules without a widget still work.
  - The collapse button shrinks a node to its title, mute, bypass and connectors (`isCollapsed`, saved with the module like its position). Collapsed nodes with several ports keep enough height for them and their labels.
  - Double-clicking the title renames the module (Enter or leaving the field saves, Escape cancels, an empty name goes back to the type's title). A named module shows the type's title below its name.
  - The level meter (`useModuleLevels`) is studio-style, per channel on a -60..0 dBFS scale: the RMS as a solid bar, the peak as a lighter bar behind it (instant rise, falling at 20 dB/s), and the highest peak as a marker held for 30 updates (about 1.5 s) that turns red at full scale. While a module isn't processed, its meter stays at zero with its last channel count, so the node doesn't change height.
  - Controls inside nodes need the `nodrag`/`nowheel` classes. `ParameterSlider` is the shared parameter control, with linear or logarithmic scales and integer slider positions, so keyboard steps are exact.
- **Graph editor (`@xyflow/react`):**
  - Nodes and edges follow the engine.
  - Connecting, deleting (Delete key or "Delete" in the node's "More" menu) and moving (the position is saved on drop) go through the API. Rejected connections, e.g. cycles, show a notification.
  - Modules snap to a 20 px grid (the background dots) unless the preference is off.
  - Dragging a selected connection's end to another port reroutes it: the new connection is created first and the old one removed only if that succeeded. Only selected connections have handles at their ends, drawn above the other connections, so a drag at a port with several connections can't take the wrong one.
  - Dropping a new connection on empty space opens a menu of module types with a matching port; the chosen module is added there and connected (its first input when the drag started at an output, its first output otherwise).
  - Right-clicking empty space opens the same menu with all module types; the chosen module is added at the click. The browser's context menu is suppressed on the graph, except in text fields.
  - Right-clicking a module or the selection opens a menu with "Group" (for modules not in a subgraph), "Remove from subgraph" (for modules in one) and "Delete", which deletes the right-clicked module, or the selected modules if it is one of them.
  - New modules (from the toolbar, near the center of the view, or from a context menu, at the click) are moved to the nearest free grid position (`findFreePosition` in `placement.ts`), keeping a grid step from other nodes. Outside subgraphs, the modules and subgraphs outside subgraphs count; a module added in a subgraph only avoids that subgraph's modules and stays below its header. The size of a new module is taken from a module of the same type on the graph, if there is one; once the new module is measured, it's placed again from the same wanted position if it overlaps (`pendingPlacement`). A new module becomes the only selected element, also when the engine's notification arrives after the response (`pendingSelection`).
  - Node cards don't clip their content, so the ports on their edges are whole and fully clickable.
  - Selected nodes aren't raised, so a selected subgraph's frame doesn't cover modules that overlap it.
- **Subgraphs** in the graph editor:
  - Ctrl+G or "Group" in a module's or the selection's context menu wraps the selected modules in a new subgraph, on the grid with room for the header. Modules already in a subgraph can't be grouped, also not together with others: the menu then has no "Group", and Ctrl+G does nothing.
  - Expanded, a subgraph is a frame in its color (Fluent palette tokens) behind its modules (React Flow parent nodes). Its header has the name (double-click to rename), fit to modules, mute, bypass, collapse and the "More" menu, and the corner at the bottom right resizes it. Frames grow to contain their modules, e.g. after one was added or expanded; "Fit to modules" puts the frame tightly around them (as when grouping), moving the modules' relative positions so they stay in place.
  - A module stays in its subgraph while it overlaps the frame and leaves it once it is dropped completely outside. Dropping a module with its center on another expanded frame moves it into that subgraph. "Remove from subgraph" in the context menu places it below the frame. Right-clicking a frame adds a module in it.
  - A module muted or bypassed by its subgraph shows it with its own switch disabled.
  - Collapsed, a subgraph is a node with a row per port that connections from or to the outside use (handle ids `in:<moduleId>:<port>` / `out:<moduleId>:<port>`, mapped back for connecting). Its modules and the connections between them are hidden.
  - The "More" menu has a "Color" submenu, the template actions (see below), "Ungroup" (the modules stay) and "Delete" (the modules are removed too). The Delete key on a selected subgraph deletes it with its modules as well, in one engine request; the engine also removes the connections of removed modules, so the editor only deletes the other selected modules and connections itself.
- **Subgraph templates** in the UI:
  - The "More" menu in a subgraph's header has "Save as template…", "Update from template…" and "Detach from template". The header shows the template's name, with "· changed" and an update button when the template's revision is higher.
  - The save dialog suggests the subgraph's template's name (or the subgraph's), so saving again updates the template; a name that a template already has offers "Replace" and saves over it. For a built-in template, it suggests "<name> (custom)" and doesn't accept a built-in template's name. Updating asks for confirmation.
  - Both add menus (the toolbar's and the graph's context menu on empty space) end with a "Templates" submenu: the templates by name (those with missing plugins disabled) and "Manage templates…". A template is added at the click, or near the center of the view from the toolbar.
  - The "Subgraph templates" dialog lists the templates with their module count, how many subgraphs use them and missing plugins, renames (double-click) and removes them. Built-in templates have a badge and can't be renamed or removed.
  - These dialogs are hosted by `SubgraphActionsProvider` outside the graph (nodes open them through `useSubgraphActions`), where React Flow's key and click handling on nodes doesn't reach them.
- **Toolbar and settings:**
  - The settings dialog has the audio settings (applied together, which rebuilds the graph) with "Restart audio" (`engine/restart-audio`) and, in the shell, "Restart engine process", the display preferences (applied right away), the plugins (install from a .zip, remove, and "Restart engine to apply" in the shell), and, in the shell, the version with "Check for updates".
  - When the shell has downloaded an update, the toolbar shows an "Update to x.y.z" button.
- **Languages:** English (the default and fallback) and German.
  - The language is the `language` preference (`/api/preferences`, null follows the system), set in the settings' "Display" section and applied right away. Without a preference, or with one the UI doesn't have, the first of the browser's languages that the UI has counts (in WebView2 that's the Windows display language), otherwise English (`resolveLanguage`). `useLanguagePreference()` at the root of the UI applies it; `<html lang>` follows.
  - `@micser/web-sdk` has the one i18next instance, which plugins get through the shared `@micser/web-sdk` module; they don't import i18next themselves. `defineTranslations(namespace, { en, de })` adds a namespace (`web` for the SPA, the plugin's name for a plugin, e.g. `main`) and returns a typed `t` and `useTranslation()`. The keys come from the English object; the German one is typed `Translations<typeof en>`, so a missing key fails the typecheck. Plural forms use i18next's `_one`/`_other` suffixes and `count`.
  - The resources are TypeScript objects next to the code: `src/Web/src/locales/{en,de}.ts` and each plugin's `Web/src/locales`.
  - A widget's `title` and `portLabels` are `LocalizedText`: a string or a function that translates it (`() => t("modules.gain")`), resolved with `localize()` when shown. Port labels default to the engine's port names.
  - Numbers follow the language (`formatNumber`, and the `decibels`, `hertz` and `milliseconds` labels), e.g. with a decimal comma in German. `ParameterSlider` re-renders when the language changes.
  - Text from the engine stays as it is: problem details, built-in template names and the names of their modules, device names.
  - Component tests render in English unless they pass `language` to `TestProviders`; the end-to-end tests run with the browser locale `en-US`.
- **Shell bridge** (`src/Web/src/shell.ts`): inside the shell's WebView2, the UI exchanges web messages with the shell (`chrome.webview`). In a plain browser it's absent, and the shell-only controls are hidden.
- **Access token:**
  - The SPA reads `#token=...` once, keeps it in `sessionStorage` and removes it from the address. The shell will open `{url}/#token={token}` from the discovery file.
  - In development, Vite proxies to the engine, which doesn't require a token.
- **Production:**
  - `dotnet publish src/Engine` copies `src/Web/dist` into `wwwroot` and the built-in plugins' `Web/dist` into `plugins/<id>/web`, so run `npm run build` first.
  - The engine serves the SPA and falls back to `index.html` for client routes, but not for `/api`, `/hubs`, `/plugins` or files.

## Plugins

- **Package.** A plugin is a folder named by its id, which is also the root of its zip package:
  - `plugin.json`: `{ "id", "name", "version", "assembly", "web", "templates" }`. `assembly` is a file name in the folder; `web` (optional) is the widget bundle's entry, e.g. `web/index.js`; `templates` (optional) is a JSON array of subgraph templates in the configuration's format (see [Engine](#engine)).
  - The assembly with its private dependencies and `.deps.json`, and the widget bundle.
- **Locations.** Built-in plugins are in the engine's `plugins` folder (`Engine:BuiltInPluginsPath`): shipped with the app and replaced by updates. User plugins are in `%LocalAppData%\Micser\plugins` (`Engine:PluginsPath`): kept across updates and removed on uninstall. A user plugin can't replace a built-in one; a duplicate id fails to load.
- **Loading** (`PluginLoader`, before the host is built):
  - Each plugin gets a non-collectible `PluginLoadContext`. Assemblies the engine has (its trusted platform assemblies: Micser.Audio, NAudio, Microsoft.Extensions.*, the framework) come from the default context, so plugins share the contract types. Everything else resolves from the plugin folder through its `.deps.json`. Plugin projects reference `Micser.Audio` with `Private="false"`, so it isn't copied.
  - `ConfigureServices` runs against a separate service collection that's copied over only if it succeeds, so a failing plugin registers nothing. Failures are listed with their error (`GET /api/plugins`, the log), and the engine starts without the plugin.
  - Plugins are fully trusted, in-process code; the install section in the UI says so. A plugin can't be unloaded, so changes take an engine restart.
  - The build-time OpenAPI document only includes the built-in plugins, so the generated client knows Main's module types.
- **Install and remove** (`PluginInstaller`): a loaded plugin's files are in use. So `POST /api/plugins` (multipart `package`, up to 100 MB) validates the zip and extracts it to `.pending/<id>` in the user plugin folder, and `DELETE /api/plugins/{id}` writes `.pending/<id>.remove` (or cancels a staged install). The next start applies them before loading. Changes are pushed as `PluginsChanged`.
- **Widget bundles.**
  - The engine serves each loaded plugin's widget folder at `/plugins/<id>/...` without the token (the browser imports them as modules) and with `Cache-Control: no-cache`. The browser loads a module URL once per page, so a changed bundle needs a reload.
  - A bundle is an ES module built in Vite library mode (`definePluginBuild()` from `@micser/web-sdk/vite`). It keeps the shared modules external: `react`, `react/jsx-runtime`, `@fluentui/react-components`, `@tanstack/react-query` and `@micser/web-sdk`. So plugins use the UI's React, theme, query cache and engine connection. Everything else, e.g. icons, is bundled.
  - The SPA's build (`src/Web/vite/sharedModules.ts`) adds one entry chunk per shared module that re-exports the UI's instance, and an import map in `index.html` that maps the module names to these chunks. Fluent UI is therefore shipped whole instead of tree-shaken (about 1.2 MB instead of 0.5 MB minified), which is fine for a UI served locally.
  - In development, the import map points to modules served by Vite. The dev server serves the widget bundles of this repository's plugins (`src/Plugins/*/plugin.json`) from their source (`src/Web/vite/workspacePlugins.ts`), so they get hot reloading; other plugins' bundles are proxied to the engine.
- **Build.** `src/Engine/BuiltInPlugins.targets` copies project references marked `OutputItemType="BuiltInPlugin"` (with `PluginId`) to `plugins/<id>` of the build and publish output, plus their `Web/dist` as `web/`. The engine tests import it too and also reference Main directly; the loader then shares the referenced assembly.

## Shell

- **Engine discovery:** `EngineLocator` reads the discovery file and trusts it only if its process is a running `Micser.Engine` that answers `/api/health`. The file stays behind after a crash.
- **Supervision:** `EngineSupervisor` polls the engine (every 2 s, or 0.5 s while there is none).
  - It starts `Micser.Engine.exe` from the shell's folder (or `--engine <path>`) when none is running. The engine is started detached, so it keeps running when the shell exits.
  - It restarts a crashed engine, at most 3 times per minute.
  - It reports address and token changes to the window.
  - Without an engine executable, it only waits for a running engine (development).
- **Tray:**
  - The menu has Open, "Start with Windows" (`HKCU\...\Run` value `Micser` = `"<shell>" --minimized`), Close and Exit Micser.
  - Its texts, the notifications and the window's message boxes and status page come from `Strings.resx` and `Strings.de.resx` (the `Strings` class is generated at build time; German is a satellite assembly in `de/`). `ShellLanguage` picks the language like the UI: the UI's language preference, which the UI passes on with a `setLanguage` message and the shell keeps in `%LocalAppData%\Micser\language.json` for the next start, otherwise the Windows display language if the shell has it, otherwise English. The tray menu changes right away.
  - "Close" exits the shell only, and the audio keeps running. "Exit Micser" stops the engine via `POST /api/engine/shutdown` and waits for it to exit.
  - A second shell start signals the first through a named event (`Local\Micser.Shell`), which shows its window.
- **Window:**
  - It's created on demand and disposed on close, which frees the WebView2 processes.
  - Position, size and maximized state are kept in `%LocalAppData%\Micser\shell.json`.
  - It shows `{engine url}/#token={token}`, or the `--ui <url>` override with the engine's token (Vite in development), and re-navigates when the engine changes. While no engine is available, a status page is shown.
  - Links that open new windows go to the default browser. A missing WebView2 runtime leads to a download prompt.
  - The browser's default context menu is off.
  - Web messages from the loaded UI (and only from its origin): `getState`, `checkForUpdates` (answered with `updateCheck`), `installUpdate`, `restartEngine`, and for the driver `installDriver`, `setCableCount`, `setCableLayout`, `updateDriver` and `uninstallDriver`, and `setLanguage`. The shell sends `state` (version, whether it can update, a running check, the pending update, whether it can restart the engine, the driver's status) on request and whenever it changes.
- **Restarting the engine process:** `EngineSupervisor.RestartEngineAsync` stops the engine gracefully and lets supervision start a new one; it doesn't count toward the crash restart limit. Only available when the shell can start the engine (not in development).
- **Updates** are run by `UpdateController` (see [Packaging and updates](#packaging-and-updates)), shared by the tray and the window.
- **Logs:** `%LocalAppData%\Micser\logs\shell-*.log` (Serilog). Fatal startup errors also show a message box.

## Development orchestration

- `aspire start` (or `aspire run`) runs `tools/AppHost`; `aspire.config.json` at the root points to it.
  - `engine` gets an Aspire-assigned port. The endpoint isn't proxied, because the engine's `Urls` setting would override `ASPNETCORE_URLS`, so the AppHost sets `Urls` itself. Its health check is `/api/health`.
  - `web` is the Vite dev server, with `MICSER_ENGINE_URL` set to the engine's endpoint. Aspire runs `npm install` in `src/Web` first, which installs the workspace at the root.
  - `shell` is started from the dashboard only, with `--ui` pointing to Vite.
- Logs, traces and metrics reach the dashboard through `Micser.ServiceDefaults`. Serilog keeps its own sinks and forwards to the OpenTelemetry logger provider (`writeToProviders`). Without `OTEL_EXPORTER_OTLP_ENDPOINT` nothing is exported.
- The manual workflow (`dotnet run` on port 5080 plus `npm run dev`) still works.

## Packaging and updates

- **Build:** `eng/pack.ps1 -Version x.y.z` builds the web UI, publishes engine and shell self-contained (win-x64) into one folder, and packs it with `vpk` (a local dotnet tool) into `artifacts/releases`.
  - The output is `Micser-win-Setup.exe`, a portable zip, and full and delta packages.
  - Both apps use the same runtime, so its files are shared. Setup is about 75 MB.
  - Nothing is code-signed yet, so SmartScreen warns on the first run.
- **Release:** pushing a tag `vX.Y.Z` runs CI, which calls `.github/workflows/release.yml` once all its jobs passed.
  - It downloads the previous release (the base for deltas), packs, and publishes a GitHub release.
  - Tags with a suffix (`v0.2.0-beta.1`) become pre-releases, which installed copies ignore.
- **Install:**
  - The install goes to `%LocalAppData%\Micser`: the stub `Micser.exe`, `Update.exe`, `current\` (the app) and `packages\`. The shell's local data (logs, `engine.json`, `shell.json`, WebView2) lives in the same folder.
  - Uninstalling removes the whole folder. The configuration in `%AppData%\Micser` stays.
  - Shortcuts go to the Start menu and the desktop.
- **Hooks** (`InstallHooks`; `VelopackApp.Run()` is the first call in the shell):
  - After install, autostart is enabled; it points to `current\Micser.Shell.exe`, which stays the same across updates.
  - Before an update, the old shell stops the engine gracefully, so it saves its configuration. Velopack then ends all remaining processes in the app folder.
  - Before uninstall, autostart is removed. Velopack ends the processes before this hook, so the engine is killed.
- **Updates** (`Updater`, only in an installed copy):
  - The shell checks the GitHub releases 30 s after starting and every 12 h, and downloads a newer release in the background.
  - The tray menu shows "Check for updates", or "Restart to update to x.y.z" once one is downloaded; that stops the engine and restarts into the new version.
  - A downloaded update is also applied on the next shell start.
  - `MICSER_UPDATE_SOURCE` points the shell at another feed (a local folder or URL) for testing.
  - Velopack logs to `%LocalAppData%\velopack\velopack_Micser.log`.
- **Driver:** Velopack can't run elevated steps, so the VAC driver isn't part of the app's installation. `eng/pack.ps1 -DriverPackage <dir>` puts the driver and `DriverUtility` in the release's `driver` folder, and the shell installs and changes the driver from the settings by running `DriverUtility` elevated (see [VAC driver](#vac-driver)). It installs machine-wide in `%ProgramFiles%\Micser\Driver` with its own "Apps and Features" entry, so uninstalling Micser leaves the driver. The release workflow doesn't bundle it yet: that needs a signed driver (phase 4); without a driver package the app hides the driver settings.

## VAC driver

The plan (signing, installation, phases) is in the [driver plan](https://claude.ai/code/artifact/384fcc8a-3a37-43ab-bdbd-3f1f9b15c48e).

- **Base.** `src/Vac` starts from Microsoft's SimpleAudioSample (SysVAD cut down to one speaker and one microphone, PortCls WaveRT, KMDF for the adapter). The old WaveCyclic driver (`src/Driver`) is in git history.
- **Cables.** One device with up to 16 cables. The count is the `CableCount` value in the device's hardware key (`Device Parameters`; the INF sets 1 without overwriting an existing value) and is read when the device starts. Changing it takes a device restart (PnP), which DriverUtility will do; there is no control device or IOCTL. `CAdapterCommon::InstallCables` creates the filters of each side from the template pairs in `minipairs.h`, with reference strings like `WaveRender2`; their interface settings (`EP\0 ...`) are copied from the INF's template interfaces (`WaveRender`, `TopologyRender`, `WaveCapture`, `TopologyCapture`).
- **Names.** Endpoints are named "Cable N Input" (render) and "Cable N Output" (capture), shown as e.g. "Cable 1 Input (Micser Virtual Audio Cable)". The topology bridge pins have a name GUID, and the topology miniports implement `IPinName` to return the name per cable. Windows ignores the pin name for `KSNODETYPE_SPEAKER`, so the render bridge pin is a `KSNODETYPE_LINE_CONNECTOR`. Windows keeps an endpoint's name once it exists.
- **Cable.** A cable is one render endpoint and one capture endpoint. `CCable` is a lock-free single-producer/single-consumer ring between the render stream (writes what the client played) and the capture stream (reads it into the client's buffer). Both streams advance their positions on the same QPC clock, so there is no drift to correct: the fill only varies with timer jitter. Capture outputs silence until 10 ms are buffered and again after an underrun, and skips the oldest data above 30 ms. The render side drops data while no capture stream runs. The ring holds 32-bit integer samples, and positions and latencies count samples; it only ever drops or skips whole frames, so the channel order can't shift.
- **Formats.** Both sides offer 48 kHz integer PCM in the cable's layout (stereo unless set otherwise, see Surround below) at 32 bits (the device format, which Windows mixes into), 24 valid bits in 32, packed 24 bits and 16 bits. A 16-bit stereo `WAVE_FORMAT_PCM` is accepted too.
  - The sides can use different formats, e.g. a 16-bit exclusive player and Windows' 32-bit mix for the recording apps. `CCable` converts: 16- and 24-bit samples become the high bits of the ring's 32-bit samples. On the way out they're rounded to nearest and clamped at full scale, without dither. 32-bit (and 24-in-32) streams are copied. A round trip at the same depth is bit-exact.
  - Data range intersection returns the first of these formats that the client's range allows; format validation accepts any of them.
  - Float isn't offered: Windows marks endpoints with a float device format "not present", and the Advanced tab of the Sound control panel would offer it. 32-bit PCM keeps a float signal in [-1, 1] at least as precisely as float itself; a signal above full scale clips at the cable, as on a real device.
  - Other sample rates (e.g. 44.1 kHz for a bit-perfect player) would need resampling in the kernel and aren't offered. The Advanced tab lists the four bit depths, so a user can pick 16 or 24 bits as the shared-mode format; `DriverUtility` only checks the channels and mask, so that choice stays.
  - `AudioHarness formats` on a stereo cable in the VM (2026-10-03) found that shared mode with the audio engine's conversion (`AUTOCONVERTPCM`, which WinMM, DirectSound, XAudio2 and Media Foundation use) accepts every tested format on both sides: 16, 44.1, 48 and 96 kHz; 16-, 24- and 32-bit int and float; mono, stereo and 5.1. Plain shared mode needs 48 kHz and the cable's channels, but any sample type.
  - With the bit depths (2026-10-04; stereo and 5.1 cables):
    - Exclusive mode accepts 16, 24, 24-in-32 and 32 bits in the cable's channels, and still rejects float and 44.1 kHz.
    - A 1 kHz tone arrived at the expected level and frequency in every mixed pair, e.g. 16-bit exclusive in and 24-bit exclusive out, 24-bit exclusive in and shared float out, shared float in and 16-bit exclusive out.
    - A counting sequence in exclusive mode (16 → 16, 24 → 24, 24-in-32 → 24 and 16 → 32 bits) arrived bit-exact on every channel, about 95,000 frames each.
    - The latency test through the cable missed no burst in 60 s, at a round trip of about 80 ms.
- **Surround.** Each cable has a layout: stereo, 5.1 (side speakers, mask `0x60F`, Windows' usual 5.1) or 7.1 (`0x63F`), for routing games and movies through Micser to surround speakers or headphones.
  - The layout is the channel count in `Cable<N>Channels` in the hardware key (anything but 6 or 8 is stereo), read when the device starts. `CAdapterCommon::InstallCableEndpoint` gives each endpoint its own copy of the streaming pin's format and mode with the cable's channels and mask, and the render jack reports the mask (`ENDPOINT_MINIPAIR::ChannelMask`). The static data ranges allow up to 8 channels; data range intersection and format validation use the endpoint's formats. Both sides use the layout, so the cable never mixes channels; 5.1 and 7.1 cables get a 512 KiB ring buffer (about 340 ms of 7.1).
  - Windows keeps an endpoint's device format across device restarts (below), so `DriverUtility set-layout` restarts the device and then sets both endpoints' device format with `IPolicyConfig::SetDeviceFormat` (`CableEndpoints`, source-generated COM). It finds a cable's endpoints by their KS filter (`{233164c8-1b2c-4c7d-bc68-b671687a2567},1`, e.g. `…\root#media#0000#{…}\waverender1`; observed, not documented). `install`, `update` and `set-count` do the same after their restart. When the restart needs a reboot (a cable is in use), `status` reports the cable's formats as not matching, and the shell runs `sync-formats` when it reads the status; that needs no elevation.
  - The settings show a layout per cable and warn that apps recording a surround cable in stereo get Windows' quieter downmix.
  - The design comes from a spike in the VM (2026-10-03; one layout for all cables from a registry value, the format tables patched when the cables are installed), which found:
  - Windows accepts 7.1 32-bit PCM (mask `0x63F`) as the device format, and the mix format follows (8 channels). All 8 channels arrived where they were played, at unchanged level. 5.1 played into the 7.1 cable landed 1:1 on channels 1–6. The ring buffer needs 512 KiB (about 340 ms of 7.1).
  - Stereo played into a 7.1 cable (Windows' upmix on the render side) arrives on front left and right at unchanged level.
  - A stereo app recording from a 7.1 cable gets Windows' capture downmix: the front channels 13.5 dB quieter, with center (-16.5 dB), LFE and the surrounds mixed in. Micser captures all channels and folds them itself (`ChannelMixer`), so it isn't affected, but e.g. Discord or OBS recording the cable in stereo would be. Surround therefore stays opt-in per cable, and the settings should say so.
  - A device restart doesn't update an existing endpoint's device format (`PKEY_AudioEngine_DeviceFormat`): after a layout change the endpoint keeps the old format, which the driver no longer offers, and can't be opened (`AUDCLNT_E_UNSUPPORTED_FORMAT`), in both directions. Only the audio service can write that property, and `ResetDeviceFormat` keeps it because the INF sets no default format. `IPolicyConfig::SetDeviceFormat` (the undocumented interface behind the Advanced tab of the Sound control panel) on both endpoints fixes it immediately and works from an elevated process, so a layout change has to call it after the device restart.
  - Not tested yet: a game choosing its output from the cable. Windows doesn't set the endpoint's `PKEY_AudioEndpoint_PhysicalSpeakers`, which some games might read instead of the mix format.
- **DRM.** Render streams with `CopyProtect` rights don't write into the cable.
- **Build.** The WDK and SDK come from NuGet (`src/Vac/packages.config`, restored by `eng/build-vac.ps1`); the build also needs the WDK component of Visual Studio (`Microsoft.Windows.DriverKit`) and the Spectre-mitigated libraries, and the 64-bit MSBuild because the WDK packages only ship 64-bit host tools. x64 and ARM64, warnings as errors, Spectre mitigation, InfVerif `/w` after each build. Minimum Windows 10 2004 (19041) because of `ExAllocatePool2`. CI builds it on the `windows-2025-vs2026` image.
- **Device.** One root-enumerated device, hardware ID `ROOT\MicserVac`, driver `MicserVac.sys` (the old driver's `ROOT\Micser.Vac.Driver` isn't kept).
- **Testing.** Builds are test-signed with the WDK test certificate. `eng/deploy-vac-vm.ps1` installs them in a Hyper-V VM with test signing on (PowerShell Direct, `devcon`), and with `-TestSeconds` runs `AudioHarness latency` from cable input to cable output there.
  - Every dev build has the same `DriverVer`, so PnP keeps using an older package from the driver store and `devcon update` still reports success. The script therefore removes the device and all Micser packages before each install.
  - In an enhanced (RDP) session the VM only shows "Remote Audio", not the cable endpoints. PowerShell Direct and the basic console session see them.
  - A user must be signed in at the console (basic session): otherwise the audio engine renders silence for the PowerShell Direct session's streams, also into the loopback. The VM signs in automatically (Winlogon `AutoAdminLogon`, with `DevicePasswordLessBuildVersion` = 0), so this survives reboots.
  - In Debug builds, a second adapter (e.g. when a removed device still waits for a reboot) hits a breakpoint in `NewAdapterCommon` and bugchecks without a debugger; the script reboots the VM when `devcon remove` asks for it.
  - Kernel debug output can be captured with Sysinternals `dbgviewcli64 -k -v --duration <s> -l <file>` in the VM.
  - Driver Verifier (standard checks) is enabled for `MicserVac.sys` in the VM, so every test runs under it.
- **Static analysis.** `eng/codeql-vac.ps1` runs Microsoft's CodeQL driver suites (`microsoft/windows-drivers`, the WHCP `mustfix` and `recommended` suites) and fails on findings in the driver's code; CI runs it for x64. Findings in the WDK headers and `cpp/drivers/init-not-cleared` (PortCls creates the FDO) are excluded.

## Roadmap

1. **Skeleton** (done):
   - Create the new layout on .NET 10 with the props files, the npm workspace root, and a CI workflow (`dotnet build`/`test` plus the npm build and lint).
   - Remove the old WPF/Prism/engine projects. They stay available in git history (`9386ea4`).
   - Move the driver and installer to `src/Driver` and `src/Installer` as they are.
   - Move `Micser.DriverUtility` to `src/DriverUtility` as it is. It still references the removed `Micser.Common` and CSCore, so it stays outside the solution until step 2 ports it to NAudio.
2. **Audio (`src/Audio`)** (done):
   - Graph, block processing, format and resampling, and NAudio device enumeration, capture and render.
   - Port the DSP code (gain, compressor, EQ, pitch, spectrum) from the WPF version's `Micser.Plugins.Main` into `src/Plugins/Main`, using the `naudio`/`dev-temp` branches for the API mapping.
   - Port `src/DriverUtility` and add it to the solution.
   - Unit tests, plus a small console harness that routes input → gain → output as a smoke test (`tools/AudioHarness`). Its `latency` mode measures the software round trip (render + loopback capture) by cross-correlating a quiet noise burst: about 47–50 ms on a 48 kHz USB interface, from 240 ms with NAudio's default render buffer and 10 ms blocks.
   - Follow-ups:
     - Investigated and dropped: measuring the ring fill phase-corrected (from timestamps of the device callbacks) and starting streams exactly at the target. Three runs each against the per-block measurement showed no measurable difference in settling time (7-30 s either way) or swings; the scatter comes from the devices (a wireless headset delivers in irregular bursts). The corrections stay below ~2000 ppm (about 3.5 cents, inaudible), and the adaptive buffers absorb the fill swings.
     - Lower latency needs device periods below 10 ms. `IAudioClient3` low-latency mode (NAudio `WithLowLatency`) wasn't available on the tested devices and made loopback capture fail; exclusive mode would work but takes the device away from other applications.
3. **Engine (`src/Engine`)** (done): hosting, the JSON config store, module definition/module/connection/device/settings APIs, and SignalR hubs for change and module data pushes.
   - Stream recovery (watchdog and resume notification) was verified with a real sleep/resume.
4. **UI** (done): `src/Web`, `src/WebSdk` and `src/Plugins/Main/Web`: the Vite app, graph editor and widgets.
   - The build splits the libraries into their own chunks (React, Fluent UI, Fluent icons, React Flow, other dependencies), so a release only changes the small app chunk (about 65 kB, with the translations) and the libraries stay cached. The total is still about 1 MB, which is fine for a UI served by the local engine.
   - Unit and component tests with Vitest (see Defaults):
     - Logic: placement and subgraph geometry, the level meter's peaks and holds (`meterState.ts`), and the API fetch and access token.
     - `EngineConnection`, against a fake SignalR hub: debounced updates, the engine events' cache patches, live data and level subscriptions, and reconnecting.
     - The engine hooks, the shell bridge, plugin bundle loading and `PluginsProvider`, `useAddModule`, and the SPA's Vite plugins.
     - Components: `ParameterSlider`, `ModuleTitle`, `LevelMeter` and Main's widgets.
   - End-to-end tests with Playwright against the engine and Vite (see Defaults), 59 tests in about 1.3 min:
     - The app: the toolbar's module menu, adding and selecting, changes from the API and a second window shown live.
     - Modules: widget edits, mute, volume, collapse, rename, moving on and off the grid, deleting by menu and key.
     - Connections: connecting by drag, a rejected cycle, deleting, and dropping on empty space to add a connected module.
     - Context menus, subgraphs (grouping, collapse with proxy ports, mute, rename, ungroup, delete), templates (save, replace, add, the templates dialog) and the settings (preferences, plugins).
     - The language: from the preference, changed in the settings, from the browser's language, and passed to the shell.
     - Plugins: installing a package through the settings, the restart, the plugin's module with its widget (through the import map), removing it with another restart, the shell's "Restart engine to apply", and a rejected file.
     - The shell-only controls against `FakeShell`: hidden in a browser, the toolbar's and the settings' update controls, the update check, restarting the engine process, and the virtual cable settings (install, count, layouts, update, uninstall, problem, busy and unreadable states).
   - Not covered: the shell's side of the web messages (`MainForm`), which needs WebView2 and the WinForms window.
5. **Shell (`src/Shell`)** (done): tray, WebView2 window, engine launch, discovery and supervision, autostart.
   - Verified: engine start, UI and token handoff, single instance, crash restart, the tray's Close and Exit Micser, and restarting the engine process from the UI.
6. **Packaging and updates** (done): Velopack setup and delta updates from GitHub releases, a release workflow, and install, update and uninstall hooks in the shell.
   - Verified with the published releases 0.1.0 and 0.1.1: install, uninstall and reinstall, the release workflow with a delta package, and the tray's "Restart to update" with a graceful engine stop.
   - The UI's "Update to x.y.z" button (from 0.3.0) was verified with the update to 0.4.0.
   - Follow-ups:
     - Code signing (`vpk pack --signParams`).
7. **VAC driver** (in progress, see [VAC driver](#vac-driver)):
   - Phase 1, spike (done): one cable from SimpleAudioSample with a ring buffer between its sides. Builds for x64 and ARM64 and passes InfVerif. In the VM, `AudioHarness latency` through the cable found all 1199 bursts in 10 minutes, without a cable underrun or skip; after settling, the round trip (harness render and capture buffering plus the cable) stayed at 93.8 ms.
   - Phase 2 (done): up to 16 cables from the device's hardware key, reload by device restart, endpoint names per cable, Driver Verifier and CodeQL clean, `src/Driver` removed. Verified in the VM with 3 cables: audio through each cable without missed bursts (cables 1 and 3 also under Driver Verifier), nothing from cable 2 on cable 1, count changes by device restart, and install, restart and removal (driver unload) under Driver Verifier without findings.
   - Phase 3 (done): `DriverUtility status | install | update | set-count | set-layout | sync-formats | uninstall` (Native AOT exe, SetupAPI; copies itself and the package to `%ProgramFiles%\Micser\Driver` with an "Apps and Features" entry, logs to `%ProgramData%\Micser\logs`). The shell runs it elevated with the engine paused (`EngineSupervisor.RunWithoutEngineAsync`), reads its status at start (tray notice for a newer bundled driver), and the settings dialog has a "Virtual audio cables" section. `eng/pack.ps1 -DriverPackage <dir>` bundles the driver in the release's `driver` folder. Verified in the VM: install, count changes and uninstall through the UI, and an update from 1.0.0.0 to 1.0.1.0.
   - Phase 4: EV certificate, attestation signing and code signing of the Velopack output.
   - Surround (done): a stereo, 5.1 or 7.1 layout per cable (see [VAC driver](#vac-driver)) in the driver, `DriverUtility set-layout` and `sync-formats`, the shell and the settings. Verified in the VM with 2 cables (`AudioHarness formats` after each step):
     - 7.1 and 5.1 pass every channel exactly while the other cable stays stereo (its format table unchanged by the 8-channel data range); switching back to stereo restores exclusive mode.
     - `set-layout` fixes the endpoints' formats within about a second of the restart. With a stream open it needs a reboot (exit code 3010); afterwards `status` reports the mismatch and `sync-formats` fixes it.
     - `sync-formats` works at medium integrity (a limited token in the console session, as the shell runs it).
     - Uninstalling and reinstalling after a 7.1 cable starts stereo with matching formats.
     - The settings section, checked in Edge with a fake `chrome.webview`, shows the layouts and sends `setCableLayout`.
8. **Runtime-loaded plugins** (done, see [Plugins](#plugins)): the plugin loader, Main loaded from `plugins/Main`, widget bundles sharing modules through an import map, install and removal from the UI, and modules of missing plugins kept in the configuration.
   - Verified with a published engine in Edge: Main's widgets from its bundle, with edits reaching the engine; a zip plugin with its own module and widget (a Fluent badge in the UI's theme) installed through the settings dialog and loaded after a restart; its removal, which kept its module in the configuration; and the dev server serving Main from source.
   - Follow-ups: published SDK packages (NuGet for `Micser.Audio`, npm for `@micser/web-sdk`), TypeScript types for other plugins' module states, and version compatibility checks between plugins and the engine.
9. **Subgraphs** (done): grouping modules into named, colored, collapsible subgraphs with their own mute and bypass (see [Engine](#engine) and [UI](#ui)).
   - Verified in headless Chrome against a dev engine: grouping with Ctrl+G, rename, color, mute (members show it), collapse with proxy ports, moving modules in and out by drag and the context menu, adding a module in a frame, moving and resizing, and ungrouping with Delete.
   - Templates (phase 2): saving, instantiating, updating and managing subgraph templates. Verified in headless Chrome: saving a subgraph, adding the template from the graph's menu, saving over it from the source with the suggested name (the instance showed "changed"), updating the instance (matched modules kept their ids and outside connection), and renaming and removing in the dialog, which cleared the references.
