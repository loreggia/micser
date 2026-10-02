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
| 8 | Packaging | **Velopack** (see [Packaging and updates](#packaging-and-updates)): a per-user install without admin rights, with delta updates from GitHub releases. The old WixSharp installer was removed; it's in git history. | WixSharp/WiX 3 + custom `HttpUpdateService` |

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
- **Autostart:** an `HKCU\...\Run` entry for the shell. The shell launches the engine if it isn't running (see [Shell](#shell)).
- **UI libraries:** Fluent UI React v9 (light/dark following the OS), TanStack Query for engine state, Orval for the API client (see [UI](#ui)).

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
  ServiceDefaults/            Micser.ServiceDefaults: OpenTelemetry setup, exported only when run from the AppHost
  DriverUtility/              Micser.DriverUtility: VAC driver install/config CLI (standalone)
  Vac/                        Micser.Vac: VAC driver (PortCls WaveRT, C++), built by eng/build-vac.ps1, not in Micser.slnx
tests/                        mirrors src/
  Audio/                      Micser.Audio.Tests
  Engine/                     Micser.Engine.Tests
  Plugins/Main/               Micser.Plugins.Main.Tests
tools/                        dev-only programs, in Micser.slnx but never shipped
  AppHost/                    Micser.AppHost: Aspire AppHost that runs engine, Vite and (on demand) shell with a dashboard
  AudioHarness/               routes a real input through a gain module to a real output and prints buffer statistics
eng/                          scripts: pack.ps1 (Velopack release), build-vac.ps1 (driver), deploy-vac-vm.ps1 (driver into a test VM)
docs/
```

**Plugin layout.** A plugin is one folder containing both halves: the .NET project and its `Web/` npm package. Adding or changing a module touches one folder, and the folder is already the unit that runtime-loaded plugins will ship as later. Widget tests sit next to the widgets (`*.test.tsx`).

**Module contract.** The engine exposes module definitions (type name, input/output connectors, state schema) through the API. Widgets are registered by module type name and read connectors from the definition instead of hard-coding them. This avoids the name drift seen on `dev` (`Output` vs. `Output01`).

**Dependencies** go one way:
- .NET: `Plugins.Main → Audio`, `Engine → Audio, Plugins.Main, ServiceDefaults`, and `Shell → nothing` (it talks to the engine only over HTTP). `DriverUtility` is standalone.
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
- **System volume.** A module can follow the volume and mute of Windows' default output device (`useSystemVolume`, as on `master`), so the volume keys control routes into devices that ignore the Windows volume (e.g. a loopback or virtual cable). `SystemVolume` watches the default device's endpoint volume and switches with the default device; `AudioHost` then sets the modules' volume (the device's level in dB as a linear gain, so loudness matches) and mute, persists and broadcasts them. The UI disables the module's volume and mute controls meanwhile.
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
  - Plugins register module types with `services.AddAudioModule<TModule, TState>("Type")`.
  - Every module implements `IStatefulModule<TState>`. `TState` is an immutable record with data annotations, and each module type has its own.
  - Modules raise `StateChanged` when they change their own state (e.g. a device module switching ports), so the engine persists and broadcasts it.
  - Modules with live data (spectrum, device stream statistics) implement `IModuleDataSource`.
- **`ModuleDto`** is polymorphic by `type`: `ModuleDto<TState>` per module type, registered at runtime (`ModuleCatalog`, `EngineJson`).
  - It carries the id, name, UI position, volume, mute, bypass (effects only) and the typed `state`.
  - The API, SignalR and the config file all use the same schema. OpenAPI shows it as `anyOf` with a discriminator mapping, so a generated TS client narrows `state` by `type`.
- **API** (`/api`, see `src/Engine/Endpoints/ApiEndpoints.cs`):
  - `health`, `module-types` (ports and default state), `modules` (create with defaults, full update with `PUT`, delete), `connections`, `devices`, `engine` (status, start, stop, restart-audio, settings, shutdown), and `preferences`.
  - `engine/restart-audio` rebuilds the graph with the current settings, which reopens all device streams with fresh buffers.
  - `preferences` are the web UI's preferences (stream statistics, grid snapping). They live in the engine's configuration because the UI's origin (the engine's random port) changes on every start, so browser storage would lose them. Changes are pushed as `PreferencesChanged`.
  - Errors are problem details: 400 with `errors` keyed by camelCase property path (e.g. `state.bands[1].frequency`), 404, and 409 for cycles and duplicates.
- **Hub** (`/hubs/engine`):
  - Pushes `ModuleChanged`, `ModuleRemoved`, `ConnectionAdded`, `ConnectionRemoved`, `DevicesChanged` and `StatusChanged` to all clients, in the order they happened.
  - `Subscribe(moduleId)` / `Unsubscribe(moduleId)` start and stop `ModuleData` pushes (20 per second) for modules with live data.
  - `SubscribeLevels()` / `UnsubscribeLevels()` start and stop `Levels` pushes (20 per second): the levels of all processed modules in one message (`PortLevelsDto` per port, linear amplitude). Hub payloads aren't in the OpenAPI document, so the web SDK declares `PortLevels` itself.
- **Configuration** (`%AppData%\Micser\config.json`, `Engine:ConfigPath`):
  - It's versioned, with saves debounced (500 ms) and written atomically.
  - An unreadable file is moved to `config.json.<timestamp>.bak`, and the engine starts empty.
  - Modules of unknown type, invalid modules and dangling connections are skipped when loading.
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
  - Module updates (`useModuleUpdate`) show immediately and go to the engine debounced (80 ms, last value wins). Engine echoes are ignored while an update is pending, so controls don't jump back.
  - `useModuleData(moduleId)` subscribes to live data (spectrum, stream statistics).
- **Widgets:**
  - Plugins export `defineWidget({ moduleType, title, component })` from their `Web` package. The component receives the typed module (`WidgetProps<"Gain">`) and a `setState` function.
  - The graph node around it is generic: title, mute, bypass (if `supportsBypass`), remove, volume, a level meter, and connectors from the engine's module type. Modules without a widget still work.
  - Double-clicking the title renames the module (Enter or leaving the field saves, Escape cancels, an empty name goes back to the type's title). A named module shows the type's title below its name.
  - The level meter (`useModuleLevels`) is studio-style, per channel on a -60..0 dBFS scale: the RMS as a solid bar, the peak as a lighter bar behind it (instant rise, falling at 20 dB/s), and the highest peak as a marker held for 30 updates (about 1.5 s) that turns red at full scale. While a module isn't processed, its meter stays at zero with its last channel count, so the node doesn't change height.
  - Controls inside nodes need the `nodrag`/`nowheel` classes. `ParameterSlider` is the shared parameter control, with linear or logarithmic scales and integer slider positions, so keyboard steps are exact.
- **Graph editor (`@xyflow/react`):**
  - Nodes and edges follow the engine.
  - Connecting, deleting (Delete key or the node's remove button) and moving (the position is saved on drop) go through the API. Rejected connections, e.g. cycles, show a notification.
  - Modules snap to a 20 px grid (the background dots) unless the preference is off.
  - Dragging a connection's end to another port reroutes it: the new connection is created first and the old one removed only if that succeeded.
  - Dropping a new connection on empty space opens a menu of module types with a matching port; the chosen module is added there and connected (its first input when the drag started at an output, its first output otherwise).
  - Node cards don't clip their content, so the ports on their edges are whole and fully clickable.
- **Toolbar and settings:**
  - A split button restarts the audio (`engine/restart-audio`); its menu also restarts the engine process when running in the shell.
  - The settings dialog has the audio settings (applied together, which rebuilds the graph), the display preferences (applied right away), and, in the shell, the version with "Check for updates".
  - When the shell has downloaded an update, the toolbar shows an "Update to x.y.z" button.
- **Shell bridge** (`src/Web/src/shell.ts`): inside the shell's WebView2, the UI exchanges web messages with the shell (`chrome.webview`). In a plain browser it's absent, and the shell-only controls are hidden.
- **Access token:**
  - The SPA reads `#token=...` once, keeps it in `sessionStorage` and removes it from the address. The shell will open `{url}/#token={token}` from the discovery file.
  - In development, Vite proxies to the engine, which doesn't require a token.
- **Production:**
  - `dotnet publish src/Engine` copies `src/Web/dist` into `wwwroot`, so run `npm run build` first.
  - The engine serves the SPA and falls back to `index.html` for client routes, but not for `/api`, `/hubs` or files.

## Shell

- **Engine discovery:** `EngineLocator` reads the discovery file and trusts it only if its process is a running `Micser.Engine` that answers `/api/health`. The file stays behind after a crash.
- **Supervision:** `EngineSupervisor` polls the engine (every 2 s, or 0.5 s while there is none).
  - It starts `Micser.Engine.exe` from the shell's folder (or `--engine <path>`) when none is running. The engine is started detached, so it keeps running when the shell exits.
  - It restarts a crashed engine, at most 3 times per minute.
  - It reports address and token changes to the window.
  - Without an engine executable, it only waits for a running engine (development).
- **Tray:**
  - The menu has Open, "Start with Windows" (`HKCU\...\Run` value `Micser` = `"<shell>" --minimized`), Close and Exit Micser.
  - "Close" exits the shell only, and the audio keeps running. "Exit Micser" stops the engine via `POST /api/engine/shutdown` and waits for it to exit.
  - A second shell start signals the first through a named event (`Local\Micser.Shell`), which shows its window.
- **Window:**
  - It's created on demand and disposed on close, which frees the WebView2 processes.
  - Position, size and maximized state are kept in `%LocalAppData%\Micser\shell.json`.
  - It shows `{engine url}/#token={token}`, or the `--ui <url>` override with the engine's token (Vite in development), and re-navigates when the engine changes. While no engine is available, a status page is shown.
  - Links that open new windows go to the default browser. A missing WebView2 runtime leads to a download prompt.
  - The browser's default context menu is off.
  - Web messages from the loaded UI (and only from its origin): `getState`, `checkForUpdates` (answered with `updateCheck`), `installUpdate`, `restartEngine`. The shell sends `state` (version, whether it can update, a running check, the pending update, whether it can restart the engine) on request and whenever it changes.
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
- **Release:** pushing a tag `vX.Y.Z` runs `.github/workflows/release.yml`.
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
- **Driver:** Velopack can't run elevated steps. Installing the VAC driver will need a separate elevated step, e.g. `DriverUtility` started from the app. The removed WixSharp installer's driver custom actions (git history, `src/Installer`) can serve as reference.

## VAC driver

The plan (signing, installation, phases) is in the [driver plan](https://claude.ai/code/artifact/384fcc8a-3a37-43ab-bdbd-3f1f9b15c48e).

- **Base.** `src/Vac` starts from Microsoft's SimpleAudioSample (SysVAD cut down to one speaker and one microphone, PortCls WaveRT, KMDF for the adapter). The old WaveCyclic driver (`src/Driver`) is in git history.
- **Cables.** One device with up to 16 cables. The count is the `CableCount` value in the device's hardware key (`Device Parameters`; the INF sets 1 without overwriting an existing value) and is read when the device starts. Changing it takes a device restart (PnP), which DriverUtility will do; there is no control device or IOCTL. `CAdapterCommon::InstallCables` creates the filters of each side from the template pairs in `minipairs.h`, with reference strings like `WaveRender2`; their interface settings (`EP\0 ...`) are copied from the INF's template interfaces (`WaveRender`, `TopologyRender`, `WaveCapture`, `TopologyCapture`).
- **Names.** Endpoints are named "Cable N Input" (render) and "Cable N Output" (capture), shown as e.g. "Cable 1 Input (Micser Virtual Audio Cable)". The topology bridge pins have a name GUID, and the topology miniports implement `IPinName` to return the name per cable. Windows ignores the pin name for `KSNODETYPE_SPEAKER`, so the render bridge pin is a `KSNODETYPE_LINE_CONNECTOR`. Windows keeps an endpoint's name once it exists.
- **Cable.** A cable is one render endpoint and one capture endpoint. `CCable` is a lock-free single-producer/single-consumer ring between the render stream (writes what the client played) and the capture stream (reads it into the client's buffer). Both streams advance their positions on the same QPC clock, so there is no drift to correct: the fill only varies with timer jitter. Capture outputs silence until 10 ms are buffered and again after an underrun, and skips the oldest data above 30 ms. The render side drops data while no capture stream runs.
- **Formats.** Both sides only offer 48 kHz stereo 32-bit PCM, so the cable copies bytes; each pin's data range intersection returns that one format. Float would avoid a conversion, but Windows marks endpoints with a float device format "not present". 32-bit PCM keeps a float signal in [-1, 1] at least as precisely as float itself. Converting between different formats is an open decision.
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
   - Remove the old WPF/Prism/engine projects. They stay available in git history and on `master`.
   - Move the driver and installer to `src/Driver` and `src/Installer` as they are.
   - Move `Micser.DriverUtility` to `src/DriverUtility` as it is. It still references the removed `Micser.Common` and CSCore, so it stays outside the solution until step 2 ports it to NAudio.
2. **Audio (`src/Audio`)** (done):
   - Graph, block processing, format and resampling, and NAudio device enumeration, capture and render.
   - Port the DSP code (gain, compressor, EQ, pitch, spectrum) from `master`'s `Micser.Plugins.Main` into `src/Plugins/Main`, using the `naudio`/`dev-temp` branches for the API mapping.
   - Port `src/DriverUtility` and add it to the solution.
   - Unit tests, plus a small console harness that routes input → gain → output as a smoke test (`tools/AudioHarness`). Its `latency` mode measures the software round trip (render + loopback capture) by cross-correlating a quiet noise burst: about 47–50 ms on a 48 kHz USB interface, from 240 ms with NAudio's default render buffer and 10 ms blocks.
   - Follow-ups:
     - Investigated and dropped: measuring the ring fill phase-corrected (from timestamps of the device callbacks) and starting streams exactly at the target. Three runs each against the per-block measurement showed no measurable difference in settling time (7-30 s either way) or swings; the scatter comes from the devices (a wireless headset delivers in irregular bursts). The corrections stay below ~2000 ppm (about 3.5 cents, inaudible), and the adaptive buffers absorb the fill swings.
     - Lower latency needs device periods below 10 ms. `IAudioClient3` low-latency mode (NAudio `WithLowLatency`) wasn't available on the tested devices and made loopback capture fail; exclusive mode would work but takes the device away from other applications.
3. **Engine (`src/Engine`)** (done): hosting, the JSON config store, module definition/module/connection/device/settings APIs, and SignalR hubs for change and module data pushes.
   - Stream recovery (watchdog and resume notification) was verified with a real sleep/resume.
4. **UI** (done): `src/Web`, `src/WebSdk` and `src/Plugins/Main/Web`: the Vite app, graph editor and widgets.
   - The build splits the libraries into their own chunks (React, Fluent UI, Fluent icons, React Flow, other dependencies), so a release only changes the small app chunk (about 40 kB) and the libraries stay cached. The total is still about 1 MB, which is fine for a UI served by the local engine.
   - Follow-ups:
     - Automated UI tests. The checks so far were scripted Playwright runs against Edge outside the repo.
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
   - Phase 3: `DriverUtility install | update | uninstall | status`, the UI action and the version check in the shell.
   - Phase 4: EV certificate, attestation signing and code signing of the Velopack output.
8. **Later:** runtime-loaded plugins.
