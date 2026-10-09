# Architecture

Micser routes audio on Windows through a graph of modules (devices, effects, analyzers) that the user wires up in a web UI.

## Overview

- **Engine** (`src/Engine`): a headless per-user process, started at login by the shell. It runs the audio graph, so audio keeps running while the UI is closed. ASP.NET Core minimal APIs and SignalR on loopback (127.0.0.1, random port, per-session token) serve the UI; the OpenAPI document generates the UI's TypeScript client.
- **UI** (`src/Web`): a Vite + React + TypeScript SPA, served by the engine. `@xyflow/react` draws the routing graph, Fluent UI React v9 the controls (light/dark following the OS), TanStack Query holds engine state, i18next translates.
- **Shell** (`src/Shell`): a WinForms tray app with a WebView2 window showing the UI. It launches and supervises the engine and runs updates; it has no app logic.
- **Plugins** provide all module types, the built-in Main plugin included. The engine loads them at runtime, and the UI imports their widget bundles from the engine.
- **Storage**: one versioned JSON file (`%AppData%\Micser\config.json`) with the modules, connections, subgraphs, templates, settings and UI preferences.
- **Packaging**: Velopack, a per-user install without admin rights, with delta updates from GitHub releases.
- **VAC driver** (`src/Vac`): optional virtual audio cables (PortCls WaveRT), installed machine-wide by `DriverUtility`.

Stack: .NET 10 (`LangVersion latest`, nullable, central package management), `Microsoft.Extensions.Hosting` with the built-in DI, `System.Text.Json`, Serilog behind `Microsoft.Extensions.Logging`, NAudio (WASAPI) for audio I/O. Tests: TUnit and NSubstitute; Vitest and Playwright for the web.

## Layout

The top level separates product code, tests, build tooling and docs, not languages. `src/` holds everything that ships, grouped by component under short area names; one area can mix stacks. The .NET projects inside keep their full names (`src/Audio/Micser.Audio.csproj`).

```
Micser.slnx
global.json
Directory.Build.props         also excludes */Web/** and node_modules from .NET item globs
Directory.Packages.props
package.json                  npm workspaces: src/Web, src/WebSdk, src/Plugins/*/Web, tests/E2E
src/
  Audio/                      Micser.Audio: graph, module abstractions, plugin contract, NAudio device I/O
  Engine/                     Micser.Engine: host, HTTP API, SignalR hub, config store, plugin loader
  Plugins/
    Main/                     Micser.Plugins.Main: built-in modules, loaded at runtime like any plugin
      plugin.json               the plugin's manifest
      templates.json            its built-in subgraph templates
      Modules/                  device in/out, loopback, gain, compressor, EQ, pitch, spectrum
      Dsp/                      DSP helpers not covered by NAudio
      Web/                      @micser/plugin-main: widgets for these modules, built to Web/dist
  Web/                        @micser/web: Vite + React SPA (graph editor, dialogs)
  WebSdk/                     @micser/web-sdk: widget contract, shared controls, API client, test helpers
  Shell/                      Micser.Shell: tray + WebView2 window, engine launcher, updates
  ServiceDefaults/            Micser.ServiceDefaults: OpenTelemetry setup, exported only when run from the AppHost
  DriverUtility/              Micser.DriverUtility: VAC driver install/config CLI (standalone)
  Vac/                        Micser.Vac: VAC driver (C++), built by scripts/build-vac.ps1, not in Micser.slnx
tests/                        mirrors src/
  Audio/, DriverUtility/, Engine/, Plugins/Main/, Shell/
  Engine/TestPlugin/          a plugin for the plugin loader tests
  E2E/                        @micser/e2e: Playwright tests of the UI against a real engine
tools/                        dev-only programs, in Micser.slnx but never shipped
  AppHost/                    Aspire AppHost: engine, Vite and (on demand) shell with a dashboard
  AudioHarness/               routes real devices through a gain module; measures latency, probes stream formats
scripts/                      pack.ps1 (Velopack release), build-vac.ps1, codeql-vac.ps1, deploy-vac-vm.ps1 (driver),
                              repair-line-endings.mts (pre-commit hook, npm run fix-line-endings)
docs/
```

**Module contract.** The engine exposes module definitions (type name, input/output connectors, state schema) through the API. Widgets are registered by module type name and read connectors from the definition instead of hard-coding them.

**Dependencies** go one way:

- .NET: `Plugins → Audio`, `Engine → Audio, ServiceDefaults`, and `Shell → nothing` (it talks to the engine over HTTP only). `DriverUtility` is standalone. The engine's reference to a built-in plugin is build-only (`ReferenceOutputAssembly="false"`): it copies the plugin to `plugins/<id>` without compiling against it.
- npm: `plugin-* → web-sdk` and `web → web-sdk`. The SPA loads plugin widgets at runtime.

## Audio engine

- **Blocks and clock.** `AudioEngine` processes the `AudioGraph` on an MMCSS "Pro Audio" thread, one block per block duration (default 48 kHz, 240 frames = 5 ms), paced by a high-resolution waitable timer against absolute deadlines. If it falls more than 4 blocks behind, it skips ahead instead of catching up.
- **Graph.** Modules have named input and output ports. Each block, modules run in dependency order; every input port first receives the sum of its connected outputs. Cycles are rejected (per module, whatever the channels). Edits swap in a new processing plan under a short lock that the audio thread holds per block, so a module can be disposed as soon as `Remove` returns. A module that throws produces silence for that block.
- **Channel connections.** A connection can take a single channel of its output (`sourceChannel`) and add to a single channel of its input (`targetChannel`), 0-based, up to 64 channels; without them it carries the whole port. A whole connection and channel connections between the same ports can exist side by side. A single source channel is mixed in like a mono source, a single target channel gets the source mixed down like a mono target, and channel to channel is 1:1 (`ChannelMixer`). A channel the current layout doesn't have (e.g. a device with fewer channels, or none while it's unplugged) is silent; the connection stays.
- **Buffers and layouts.** Buffers are planar float32 and carry a `ChannelLayout` (channel count + WAVEFORMATEXTENSIBLE speaker mask). Layouts can differ per connection. An input port either has a fixed layout (e.g. a device output uses the device's layout) or takes it from the module's channel count (`AudioModule.ChannelCount`, 1–64; the standard speaker positions for 1, 2, 4, 6 and 8 channels, by index otherwise) or, without one, from its sources: the widest layout of those added to the whole input (a single source channel counts as mono), widened to the highest target channel of the channel connections (at least stereo when there are only channel connections). `ChannelMixer` converts between layouts:
  - mono sources go to front center, or else to front left and right;
  - mono targets get the average of all channels except LFE;
  - positional layouts map matching speakers 1:1 and fold missing ones into their neighbours (center into L/R at -3 dB, sides and backs into each other or into the fronts at -3 dB), dropping LFE;
  - everything else maps by index.
- **Volume.** Every module has `Volume` (0..1) and `IsMuted`, applied to its outputs with a ramp over one block. `EffectModule` adds `IsBypassed`. Samples aren't clamped inside the graph, only at device outputs.
- **System volume.** A module can follow the volume and mute of Windows' default output device (`useSystemVolume`), so the volume keys control routes into devices that ignore the Windows volume (e.g. a loopback or virtual cable). `SystemVolume` watches the default device's endpoint volume and switches with the default device; `AudioHost` then sets the modules' volume (the device's level in dB as a linear gain, so loudness matches) and mute, persists and broadcasts them. The UI disables the module's volume and mute controls meanwhile.
- **Port layouts.** After each block, every port publishes its buffer's layout (`AudioPort.LastLayout`, one 64-bit volatile write), so other threads can read it without locks. `GET /api/port-layouts` lists them per module (channel count and, for layouts with speaker positions, the speaker of each channel); the UI draws a module's channel connectors from them. A port that carries nothing, e.g. a device output without an open device or an unprocessed module, has 0 channels.
- **Levels.** Each output port has a `LevelMeter` that measures the output after volume and mute: per channel, the peak since the last read and the RMS smoothed over 300 ms. A module without outputs measures what it passes to `ApplyVolume` (a device output: what it plays). The audio thread updates the meters with atomics only; `AudioModule.ReadLevels()` reads and resets the peaks.
- **Devices.** Each capture and render stream decouples its device clock from the engine clock:
  - A lock-free single-producer/single-consumer ring buffer sits between them. Devices deliver and consume whole periods (10 ms in shared mode), so the initial target fill is:
    - render: one device period plus half an engine block;
    - capture: a block plus half a period (a read takes a whole block while periods arrive at any phase), at least one period, plus half a block. At 5 ms blocks both are 12.5 ms; at 20 ms blocks capture needs 35 ms.
  - The target adapts per stream (`AdaptiveTarget`): each dropout raises it by half a device period (dropouts within 1 s count as one), up to 200 ms. After 10 minutes without dropouts it steps back down, but not below a level that had a dropout within 10 minutes of stepping down to it. Device widgets show the current target in ms.
  - Device modules keep the learned target in their state (`bufferMilliseconds`, saved with the configuration), so a reopened stream (engine restart, settings change, resume) starts there instead of relearning. Selecting a different device in the widget clears it. Dropouts in a stream's first second don't count, since devices may still be settling in.
  - Dropouts only count while the engine side is active: when it hasn't read or written for 100 ms (e.g. audio switched off), the device's empty reads and full writes are ignored. Render underruns are recorded on the device thread and counted by the next write, which drops them after a pause. When processing resumes, render tops up with silence and capture discards the backlog.
  - While running, render never tops up a low buffer with silence: that cuts audible gaps that the device would have ridden through (on a wireless headset several per minute). Only a real underrun counts and grows the target. A capture stream that falls more than 3 targets behind skips the excess; that counts as a dropout (`Resyncs` in the statistics) and grows the target too.
  - The WASAPI render buffer is requested at 20 ms; NAudio's default of 200 ms would dominate the latency.
  - A windowed-sinc resampler (NAudio's `WdlResampler`) converts between the device and engine rates.
  - A PI controller (`DriftController`) adjusts the resampling ratio by up to ±0.5% to hold the target fill.
  - When the fill is far off (e.g. after a pause or at startup), the stream resynchronizes by discarding samples or inserting silence.
- **Device modules** follow their device's state and, when the device ID disappears, switch to another active device of the same adapter (e.g. a USB device plugged into a different port). Setting their state reopens the stream only when the device changes, since every module update (e.g. a volume change) sets the state again.
- **Stream recovery.** A device stream is faulted when WASAPI stopped it (e.g. the device was invalidated) or when its device delivered or took no data for 2 s (10 s before the first callback, for slow devices like Bluetooth). Loopback capture is exempt from the stall check because it gets no data while nothing plays.
  - A watchdog in each device module (every second) reopens a faulted stream and retries a selected device that isn't open, with a delay of 1 s doubling up to 30 s. The delay resets after a minute of healthy streaming, on device events and on device selection.
  - `AudioDeviceService.SystemResumed` (a `PowerRegisterSuspendResumeNotification` callback, no window needed) makes all device modules reopen their streams after sleep or hibernation, since streams can look healthy then but play or capture nothing.
- **Latency.** The software round trip (render + loopback capture, `AudioHarness latency`) is about 50 ms on a 48 kHz USB interface. Going much lower needs device periods below 10 ms: `IAudioClient3` low-latency mode (NAudio `WithLowLatency`) wasn't available on the tested devices and made loopback capture fail, and exclusive mode takes the device away from other applications. Measuring the ring fill phase-corrected from the device callbacks' timestamps made no measurable difference against the per-block measurement; the scatter comes from the devices.

## Engine

- **Plugin API.**
  - A plugin assembly has exactly one public `IAudioPlugin` (in `Micser.Audio`), whose `ConfigureServices` registers module types with `services.AddAudioModule<TModule, TState>("Type")`.
  - Every module implements `IStatefulModule<TState>`. `TState` is an immutable record with data annotations, and each module type has its own.
  - Modules raise `StateChanged` when they change their own state (e.g. a device module switching ports), so the engine persists and broadcasts it.
  - Modules with live data (spectrum, device stream statistics) implement `IModuleDataSource`.
- **`AudioHost`** is the single entry point for graph changes: it validates, applies, persists and broadcasts every change, and throws `EngineRequestException` for problem responses.
- **`ModuleDto`** is polymorphic by `type`: `ModuleDto<TState>` per module type, registered at runtime (`ModuleCatalog`, `EngineJson`).
  - It carries the id, name, UI position, collapse, subgraph, volume, mute, bypass (effects only), `showChannels`, `channelCount` (see API below) and the typed `state`.
  - The API, SignalR and the config file all use the same schema. OpenAPI shows it as `anyOf` with a discriminator mapping, so the generated TS client narrows `state` by `type`.
- **API** (`/api`, see `src/Engine/Endpoints/ApiEndpoints.cs`):
  - `health`, `module-types` (ports, default state, `supportsBypass`, and `supportsChannelCount` for types with an input without a fixed layout), `modules` (create with defaults, full update with `PUT`, delete), `connections`, `subgraphs` and `subgraph-templates` (see below), `port-layouts` (see below), `devices`, `engine` (status, start, stop, restart-audio, settings, shutdown), `plugins` (list, install a zip package, remove; see [Plugins](#plugins)), and `preferences`.
  - `engine/restart-audio` rebuilds the graph with the current settings, which reopens all device streams with fresh buffers.
  - `preferences` are the web UI's preferences (language, stream statistics, grid snapping). They live in the engine's configuration because the UI's origin (the engine's random port) changes on every start, so browser storage would lose them.
  - Connections may name channels (see [Audio engine](#audio-engine)). A module with channel connections on its side (the source of one that names a source channel, the target of one that names a target channel) has `showChannels` on, which shows its channel connectors in the UI: creating such a connection turns it on, and an update turning it off while they exist is rejected (400). A module's `channelCount` (types with `supportsChannelCount` only, ignored for others) can't be lower than the channel connections to its inputs need (400), and a connection to a channel beyond it is rejected (400). Restoring the configuration or a template fixes both instead of rejecting.
  - Errors are problem details: 400 with `errors` keyed by camelCase property path (e.g. `state.bands[1].frequency`), 404, and 409 for cycles and duplicates.
- **Subgraphs** (`SubgraphDto`) group modules in the UI: name, position, size, color, collapsed, mute and bypass. A module refers to its subgraph with `subgraphId`.
  - A member's position is relative to the subgraph, so moving a subgraph is one update. Creating a subgraph from modules (`POST /api/subgraphs`, also from another subgraph) and ungrouping one (`DELETE /api/subgraphs/{id}`; its modules stay) convert the positions. With `deleteModules=true`, the delete removes the modules and their connections too. The UI converts them when it moves a module in or out with a module `PUT`.
  - The subgraph's mute and bypass combine with each member's own (`module || subgraph`, bypass for effects only). `ModuleDto` keeps the module's own flags, so turning the subgraph's off restores them.
  - There's one level; subgraphs don't nest.
- **Subgraph templates** (`SubgraphTemplateDto`) are saved subgraphs: name, color, size, modules (with template-local ids and relative positions) and the connections between them, with their channels. Names are unique, ignoring case (409 otherwise).
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
  - Main's templates are effect chains to wire between an input and an output: "Footstep boost", "Night mode" and "Voice chat mic".
- **Hub** (`/hubs/engine`):
  - Pushes `ModuleChanged`, `ModuleRemoved`, `ConnectionAdded`, `ConnectionRemoved`, `SubgraphChanged`, `SubgraphRemoved`, `TemplatesChanged` (the whole list), `DevicesChanged`, `PluginsChanged`, `PreferencesChanged` and `StatusChanged` to all clients, in the order they happened.
  - `PortLayoutsChanged` goes to all clients too, with the layouts of the modules whose port layouts changed; the publisher of the levels checks them 20 times per second. It isn't ordered with the other events, so it can name a module that was just removed.
  - `Subscribe(moduleId)` / `Unsubscribe(moduleId)` start and stop `ModuleData` pushes (20 per second) for modules with live data.
  - `SubscribeLevels()` / `UnsubscribeLevels()` start and stop `Levels` pushes (20 per second): the levels of all processed modules in one message (`PortLevelsDto` per port, linear amplitude). Hub payloads aren't in the OpenAPI document, so the web SDK declares `PortLevels` itself.
- **Configuration** (`%AppData%\Micser\config.json`, `Engine:ConfigPath`):
  - It's versioned, with saves debounced (500 ms) and written atomically. A file of another version is treated as unreadable (below), so additions stay optional instead of raising the version: e.g. the channels of connections, which an older engine ignores, turning channel connections into whole ones.
  - An unreadable file is moved to `config.json.<timestamp>.bak`, and the engine starts empty.
  - Modules of unknown type (their plugin isn't loaded) are kept in the file as they are, with their connections, but stay out of the graph and the API; they come back with their plugin. Invalid modules and dangling connections are skipped.
  - Subgraphs and templates are kept with the modules. A module whose subgraph is missing loses its `subgraphId`. A subgraph keeps the reference to a missing template, which may be a built-in one whose plugin isn't loaded, so it's linked again when the plugin is; deleting a subgraph also takes the unknown-type modules out of it.
  - Changing the engine settings rebuilds the graph.
- **Hosting.** `Microsoft.NET.Sdk.Web` (Kestrel). It serves the built SPA from `wwwroot` and falls back to `index.html` for client routes, but not for `/api`, `/hubs`, `/plugins` or files. `dotnet publish src/Engine` copies `src/Web/dist` into `wwwroot` and the built-in plugins' `Web/dist` into `plugins/<id>/web`, so `npm run build` runs first. `AllowedHosts` is limited to `localhost;127.0.0.1` against DNS rebinding.
- **Discovery and security:**
  - The engine binds to `127.0.0.1` with a random port and writes `{ url, token, processId }` to `%LocalAppData%\Micser\engine.json` (`Engine:DiscoveryPath`). That folder is private to the user. The file is deleted on a clean shutdown; after a crash it stays, so readers must check that the process is alive.
  - `/api` and `/hubs` (except `/api/health`) require `Authorization: Bearer <token>`, or `access_token` in the query for SignalR from browsers.
  - A named semaphore (`Local\Micser.Engine`) allows one engine per session. `Engine:RequireToken` and `Engine:SingleInstance` turn these off (development, tests).
  - In development, the engine listens on the fixed address `http://127.0.0.1:5080` without requiring the token, and Vite proxies `/api` and `/hubs` to it.
- **Logging.** Serilog, configured from `appsettings*.json`; the file sink is only enabled in Production.

## UI

- **API client:**
  - The engine build writes `src/WebSdk/openapi/engine.json` (`Microsoft.Extensions.ApiDescription.Server`).
  - `npm run generate:api -w @micser/web-sdk` generates the Orval client from it: types, fetch functions and TanStack Query hooks in `src/WebSdk/src/api/generated`.
  - Both the document and the client are committed. CI fails if either is out of date.
  - `engineFetch` adds the access token and throws `EngineApiError` with the problem details.
- **State:**
  - Engine data lives in the TanStack Query cache, which never goes stale. `EngineConnection` (SignalR) patches it from engine events and refetches everything after connecting, cancelling fetches that started before (they may have missed events). An event that arrives while the same data is being fetched restarts that fetch, whose result could be older than the event. So components neither poll nor invalidate after mutations.
  - Module and subgraph updates (`useModuleUpdate`, `useSubgraphUpdate`) show immediately and go to the engine debounced (80 ms, last value wins). Engine echoes are ignored while an update is pending, so controls don't jump back.
  - `useModuleData(moduleId)` subscribes to live data (spectrum, stream statistics).
  - `usePortLayouts(moduleId)` reads a module's port layouts from the cache, which `PortLayoutsChanged` patches and `ModuleRemoved` cleans up.
  - UI preferences come from the engine (`usePreferences`), not from browser storage (see `preferences` under [Engine](#engine)).
- **Widgets:**
  - A plugin's `Web` package default-exports `definePlugin({ name, widgets })` with `defineWidget({ moduleType, title, component })` entries. The component receives the typed module (`WidgetProps<"Gain">`) and a `setState` function. Module types of plugins outside this repository aren't in the generated API types.
  - `PluginsProvider` imports the widget bundles of the loaded plugins (`webUrl` from `GET /api/plugins`) before the graph is shown. A bundle that fails to load shows a notification, and its modules render without a widget.
  - The graph node around it is generic: title, mute, bypass (if `supportsBypass`), collapse, a "More" menu with "Show channels", "Channels" (if `supportsChannelCount`) and "Delete" (also when collapsed), volume, a level meter, and connectors from the engine's module type. Modules without a widget still work.
  - **Channels.** With "Show channels" (`showChannels`), the node shows its connectors in rows below the widget, also when collapsed: per port one for the whole port (handle id `<port>`) and one per channel (`<port>:<channel>`, 0-based), labelled with the number and the speaker, e.g. "1 (L)" (`channels.ts`). The channels come from the port layouts, at least the module's channel count (stereo for inputs on Auto), and include every channel a connection uses. The item is disabled while connections use single channels of the module. "Channels" offers Auto, Mono, Stereo, Quad, 5.1, 7.1 and "Custom…" (a dialog hosted by `ModuleActionsProvider`, 1–64); counts below what the connections to single channels need are disabled.
  - The collapse button shrinks a node to its title, mute, bypass and connectors (`isCollapsed`, saved with the module like its position). Collapsed nodes with several ports keep enough height for them and their labels.
  - Double-clicking the title renames the module (Enter or leaving the field saves, Escape cancels, an empty name goes back to the type's title). A named module shows the type's title below its name.
  - The level meter (`useModuleLevels`) is studio-style, per channel on a -60..0 dBFS scale: the RMS as a solid bar, the peak as a lighter bar behind it (instant rise, falling at 20 dB/s), and the highest peak as a marker held for 30 updates (about 1.5 s) that turns red at full scale. While a module isn't processed, its meter stays at zero with its last channel count, so the node doesn't change height.
  - Controls inside nodes need the `nodrag`/`nowheel` classes. `ParameterSlider` is the shared parameter control, with linear or logarithmic scales and integer slider positions, so keyboard steps are exact.
- **Graph editor (`@xyflow/react`):**
  - Nodes and edges follow the engine.
  - Connecting, deleting (Delete key or "Delete" in the node's "More" menu) and moving (the position is saved on drop) go through the API. Rejected connections, e.g. cycles, show a notification.
  - Modules snap to a 20 px grid (the background dots) unless the preference is off.
  - Connections to or from a channel the port doesn't have right now (`isBeyondLayout`, e.g. a device without that channel, or none open) are dashed. New connections, rerouted ends and the module added from a dropped connection keep the channels of the connectors they start or end at; the new module connects with its whole first port.
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
  - Collapsed, a subgraph is a node with a row per port, or channel of one, that connections from or to the outside use (handle ids `in:<moduleId>:<port>[:<channel>]` / `out:<moduleId>:<port>[:<channel>]`, mapped back for connecting by `resolveHandle`). Its modules and the connections between them are hidden.
  - The "More" menu has a "Color" submenu, the template actions (see below), "Ungroup" (the modules stay) and "Delete" (the modules are removed too). The Delete key on a selected subgraph deletes it with its modules as well, in one engine request; the engine also removes the connections of removed modules, so the editor only deletes the other selected modules and connections itself.
- **Subgraph templates** in the UI:
  - The "More" menu in a subgraph's header has "Save as template…", "Update from template…" and "Detach from template". The header shows the template's name, with "· changed" and an update button when the template's revision is higher.
  - The save dialog suggests the subgraph's template's name (or the subgraph's), so saving again updates the template; a name that a template already has offers "Replace" and saves over it. For a built-in template, it suggests "<name> (custom)" and doesn't accept a built-in template's name. Updating asks for confirmation.
  - Both add menus (the toolbar's and the graph's context menu on empty space) end with a "Templates" submenu: the templates by name (those with missing plugins disabled) and "Manage templates…". A template is added at the click, or near the center of the view from the toolbar.
  - The "Subgraph templates" dialog lists the templates with their module count, how many subgraphs use them and missing plugins, renames (double-click) and removes them. Built-in templates have a badge and can't be renamed or removed.
  - These dialogs are hosted by `SubgraphActionsProvider` outside the graph (nodes open them through `useSubgraphActions`), where React Flow's key and click handling on nodes doesn't reach them.
- **Toolbar and settings:**
  - The settings dialog has the audio settings (applied together, which rebuilds the graph) with "Restart audio" (`engine/restart-audio`) and, in the shell, "Restart engine process", the display preferences (applied right away), the plugins (install from a .zip, remove, and "Restart engine to apply" in the shell), in the shell the "Virtual audio cables" section (see [VAC driver](#vac-driver)), and, in the shell, the version with "Check for updates".
  - When the shell has downloaded an update, the toolbar shows an "Update to x.y.z" button. Release notes ("What's new", from the update button and the settings) come from the shell and are rendered with `markdown-to-jsx` (raw HTML stays text).
- **Languages:** English (the default and fallback) and German.
  - The language is the `language` preference (null follows the system), set in the settings' "Display" section and applied right away. Without a preference, or with one the UI doesn't have, the first of the browser's languages that the UI has counts (in WebView2 that's the Windows display language), otherwise English (`resolveLanguage`). `useLanguagePreference()` at the root of the UI applies it; `<html lang>` follows.
  - `@micser/web-sdk` has the one i18next instance, which plugins get through the shared `@micser/web-sdk` module; they don't import i18next themselves. `defineTranslations(namespace, { en, de })` adds a namespace (`web` for the SPA, the plugin's name for a plugin, e.g. `main`) and returns a typed `t` and `useTranslation()`. The keys come from the English object; the German one is typed `Translations<typeof en>`, so a missing key fails the typecheck. Plural forms use i18next's `_one`/`_other` suffixes and `count`.
  - The resources are TypeScript objects next to the code: `src/Web/src/locales/{en,de}.ts` and each plugin's `Web/src/locales`.
  - A widget's `title` and `portLabels` are `LocalizedText`: a string or a function that translates it (`() => t("modules.gain")`), resolved with `localize()` when shown. Port labels default to the engine's port names.
  - Numbers follow the language (`formatNumber`, and the `decibels`, `hertz` and `milliseconds` labels), e.g. with a decimal comma in German. `ParameterSlider` re-renders when the language changes.
  - Text from the engine stays as it is: problem details, built-in template names and the names of their modules, device names.
- **Shell bridge** (`src/Web/src/shell.ts`): inside the shell's WebView2, the UI exchanges web messages with the shell (`chrome.webview`, see [Shell](#shell)). In a plain browser it's absent, and the shell-only controls are hidden.
- **Access token:** the SPA reads `#token=...` once, keeps it in `sessionStorage` and removes it from the address. The shell opens `{url}/#token={token}` from the discovery file.
- **Build.** The build splits the libraries into their own chunks (React, Fluent UI, Fluent icons, React Flow, other dependencies), so a release only changes the small app chunk and the libraries stay cached. Icons are imported per icon (`@fluentui/react-icons/svg/<name>`); the package index would make Vite load all icons (17 MB) on every page load in development.

## Plugins

- **Package.** A plugin is a folder named by its id, which is also the root of its zip package:
  - `plugin.json`: `{ "id", "name", "version", "assembly", "web", "templates" }`. `assembly` is a file name in the folder; `web` (optional) is the widget bundle's entry, e.g. `web/index.js`; `templates` (optional) is a JSON file of subgraph templates in the configuration's format (see [Engine](#engine)).
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
  - The SPA's build (`src/Web/vite/sharedModules.ts`) adds one entry chunk per shared module that re-exports the UI's instance, and an import map in `index.html` that maps the module names to these chunks. Fluent UI is therefore shipped whole instead of tree-shaken (about 1.2 MB instead of 0.5 MB minified), which is fine for a UI served locally. This list and `definePluginBuild()`'s externals must stay in sync.
  - In development, the import map points to modules served by Vite. The dev server serves the widget bundles of this repository's plugins (`src/Plugins/*/plugin.json`) from their source (`src/Web/vite/workspacePlugins.ts`), so they get hot reloading; other plugins' bundles are proxied to the engine.
- **Build.** `src/Engine/BuiltInPlugins.targets` copies project references marked `OutputItemType="BuiltInPlugin"` (with `PluginId`) to `plugins/<id>` of the build and publish output, plus their `Web/dist` as `web/`. The engine tests import it too and also reference Main directly; the loader then shares the referenced assembly.
- **Not yet there:** published SDK packages (NuGet for `Micser.Audio`, npm for `@micser/web-sdk`), TypeScript types for other plugins' module states, and version compatibility checks between plugins and the engine.

## Shell

- **Engine discovery:** `EngineLocator` reads the discovery file and trusts it only if its process is a running `Micser.Engine` that answers `/api/health`.
- **Supervision:** `EngineSupervisor` polls the engine (every 2 s, or 0.5 s while there is none).
  - It starts `Micser.Engine.exe` from the shell's folder (or `--engine <path>`) when none is running. The engine is started detached, so it keeps running when the shell exits.
  - It restarts a crashed engine, at most 3 times per minute.
  - It reports address and token changes to the window.
  - Without an engine executable, it only waits for a running engine (development).
  - `RestartEngineAsync` stops the engine gracefully and lets supervision start a new one; it doesn't count toward the crash restart limit. `RunWithoutEngineAsync` pauses the engine while the driver is changed. Both are only available when the shell can start the engine (not in development).
- **Tray:**
  - The menu has Open, "Start with Windows" (`HKCU\...\Run` value `Micser` = `"<shell>" --minimized`), Close and Exit Micser.
  - "Close" exits the shell only, and the audio keeps running. "Exit Micser" stops the engine via `POST /api/engine/shutdown` and waits for it to exit.
  - A second shell start signals the first through a named event (`Local\Micser.Shell`), which shows its window.
- **Language.** The tray menu, notifications and the window's message boxes and status page come from `Strings.resx` and `Strings.de.resx` (the `Strings` class is generated at build time; German is a satellite assembly in `de/`). `ShellLanguage` picks the language like the UI: the UI's language preference, which the UI passes on with a `setLanguage` message and the shell keeps in `%LocalAppData%\Micser\language.json` for the next start, otherwise the Windows display language if the shell has it, otherwise English. The tray menu changes right away.
- **Window:**
  - It's created on demand and disposed on close, which frees the WebView2 processes.
  - Position, size and maximized state are kept in `%LocalAppData%\Micser\shell.json`.
  - It shows `{engine url}/#token={token}`, or the `--ui <url>` override with the engine's token (Vite in development), and re-navigates when the engine changes. While no engine is available, a status page is shown.
  - Links that open new windows go to the default browser. A missing WebView2 runtime leads to a download prompt.
  - The browser's default context menu is off.
- **Web messages** (`MainForm` ↔ `src/Web/src/shell.ts`), accepted only from the loaded UI's origin: `getState`, `checkForUpdates` (answered with `updateCheck`), `installUpdate`, `getReleaseNotes` (answered with `releaseNotes`), `restartEngine`, `setLanguage`, and for the driver `installDriver`, `setCableCount`, `setCableLayout`, `updateDriver` and `uninstallDriver`. The shell sends `state` (version, whether it can update, a running check, the pending update, whether it can restart the engine, the driver's status) on request and whenever it changes.
- **Updates** are run by `UpdateController` (see [Packaging and updates](#packaging-and-updates)), shared by the tray and the window.
- **Logs:** `%LocalAppData%\Micser\logs\shell-*.log` (Serilog). Fatal startup errors also show a message box.

## Packaging and updates

- **Build:** `scripts/pack.ps1 -Version x.y.z` builds the web UI, publishes engine and shell self-contained (win-x64) into one folder, and packs it with `vpk` (a local dotnet tool) into `artifacts/releases`.
  - The output is `Micser-win-Setup.exe`, a portable zip, and full and delta packages.
  - Both apps use the same runtime, so its files are shared.
  - Nothing is code-signed yet, so SmartScreen warns on the first run. Signing would go through `vpk pack --signParams`.
- **Release:** pushing a tag `vX.Y.Z` runs CI, which calls `.github/workflows/release.yml` once all its jobs passed.
  - It downloads the previous release (the base for deltas), packs, and publishes a GitHub release.
  - Tags with a suffix (`v0.2.0-beta.1`) become pre-releases, which installed copies ignore.
- **Release notes** are written by hand in `CHANGELOG.md`, for users: changes collect under "Unreleased", which is renamed to `## X.Y.Z` before tagging.
  - `scripts/pack.ps1` takes the version's section and passes it to `vpk pack --releaseNotes`, which embeds it in the package; `vpk upload github` makes it the GitHub release body. It also ships it as `ReleaseNotes.md` in the app folder. The release workflow fails without a section (`-RequireReleaseNotes`).
  - The shell provides the notes of the installed version (`ReleaseNotes.md`) and of a downloaded update (`VelopackAsset.NotesMarkdown`, from its package). An update that skips versions shows only the target version's notes.
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
- **Driver:** Velopack can't run elevated steps, so the VAC driver isn't part of the app's installation. `scripts/pack.ps1 -DriverPackage <dir>` puts the driver and `DriverUtility` in the release's `driver` folder, and the shell installs and changes the driver from the settings by running `DriverUtility` elevated (see [VAC driver](#vac-driver)). The release workflow doesn't bundle it until the driver is attestation-signed (EV certificate); without a driver package the app hides the driver settings.

## VAC driver

- **Base.** `src/Vac` derives from Microsoft's SimpleAudioSample (SysVAD cut down to one speaker and one microphone, PortCls WaveRT, KMDF for the adapter).
- **Device.** One root-enumerated device, hardware ID `ROOT\MicserVac`, driver `MicserVac.sys`.
- **Cables.** One device with up to 16 cables. The count is the `CableCount` value in the device's hardware key (`Device Parameters`; the INF sets 1 without overwriting an existing value) and is read when the device starts. Changing it takes a device restart (PnP), which DriverUtility does; there is no control device or IOCTL. `CAdapterCommon::InstallCables` creates the filters of each side from the template pairs in `minipairs.h`, with reference strings like `WaveRender2`; their interface settings (`EP\0 ...`) are copied from the INF's template interfaces (`WaveRender`, `TopologyRender`, `WaveCapture`, `TopologyCapture`).
- **Names.** Endpoints are named "Cable N Input" (render) and "Cable N Output" (capture), shown as e.g. "Cable 1 Input (Micser Virtual Audio Cable)". The topology bridge pins have a name GUID, and the topology miniports implement `IPinName` to return the name per cable. Windows ignores the pin name for `KSNODETYPE_SPEAKER`, so the render bridge pin is a `KSNODETYPE_LINE_CONNECTOR`. Windows keeps an endpoint's name once it exists.
- **Cable.** A cable is one render endpoint and one capture endpoint. `CCable` is a lock-free single-producer/single-consumer ring between the render stream (writes what the client played) and the capture stream (reads it into the client's buffer). Both streams advance their positions on the same QPC clock, so there is no drift to correct: the fill only varies with timer jitter. Capture outputs silence until 10 ms are buffered and again after an underrun, and skips the oldest data above 30 ms. The render side drops data while no capture stream runs. The ring holds 32-bit integer samples, and positions and latencies count samples; it only ever drops or skips whole frames, so the channel order can't shift.
- **Formats.** Both sides offer 48 kHz integer PCM in the cable's layout at 32 bits (the device format, which Windows mixes into), 24 valid bits in 32, packed 24 bits and 16 bits. A 16-bit stereo `WAVE_FORMAT_PCM` is accepted too.
  - The sides can use different formats, e.g. a 16-bit exclusive player and Windows' 32-bit mix for the recording apps. `CCable` converts: 16- and 24-bit samples become the high bits of the ring's 32-bit samples. On the way out they're rounded to nearest and clamped at full scale, without dither. 32-bit (and 24-in-32) streams are copied. A round trip at the same depth is bit-exact.
  - Data range intersection returns the first of these formats that the client's range allows; format validation accepts any of them.
  - Float isn't offered: Windows marks endpoints with a float device format "not present", and the Advanced tab of the Sound control panel would offer it. 32-bit PCM keeps a float signal in [-1, 1] at least as precisely as float itself; a signal above full scale clips at the cable, as on a real device.
  - Other sample rates (e.g. 44.1 kHz for a bit-perfect player) would need resampling in the kernel and aren't offered. Shared mode with the audio engine's conversion (`AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM`, which WinMM, DirectSound, XAudio2 and Media Foundation use) accepts any rate, sample type and channel count; plain shared mode needs 48 kHz and the cable's channels; exclusive mode needs one of the integer formats above.
  - The Advanced tab lists the four bit depths, so a user can pick 16 or 24 bits as the shared-mode format; `DriverUtility` only checks the channels and mask, so that choice stays.
- **Surround.** Each cable has a layout: stereo, 5.1 (side speakers, mask `0x60F`, Windows' usual 5.1) or 7.1 (`0x63F`), for routing games and movies through Micser to surround speakers or headphones.
  - The layout is the channel count in `Cable<N>Channels` in the hardware key (anything but 6 or 8 is stereo), read when the device starts. `CAdapterCommon::InstallCableEndpoint` gives each endpoint its own copy of the streaming pin's format and mode with the cable's channels and mask, and the render jack reports the mask (`ENDPOINT_MINIPAIR::ChannelMask`). The static data ranges allow up to 8 channels; data range intersection and format validation use the endpoint's formats. Both sides use the layout, so the cable never mixes channels; 5.1 and 7.1 cables get a 512 KiB ring buffer (about 340 ms of 7.1).
  - A device restart doesn't update an existing endpoint's device format (`PKEY_AudioEngine_DeviceFormat`): after a layout change the endpoint would keep the old format, which the driver no longer offers, and couldn't be opened (`AUDCLNT_E_UNSUPPORTED_FORMAT`). Only the audio service can write that property, and `ResetDeviceFormat` keeps it because the INF sets no default format. So `DriverUtility set-layout` restarts the device and then sets both endpoints' device format with `IPolicyConfig::SetDeviceFormat` (the undocumented interface behind the Sound control panel's Advanced tab; `CableEndpoints`, source-generated COM), which works from an elevated process. It finds a cable's endpoints by their KS filter (`{233164c8-1b2c-4c7d-bc68-b671687a2567},1`, e.g. `…\root#media#0000#{…}\waverender1`; observed, not documented). `install`, `update` and `set-count` do the same after their restart. When the restart needs a reboot (a cable is in use, exit code 3010), `status` reports the cable's formats as not matching, and the shell runs `sync-formats` when it reads the status; that needs no elevation.
  - A stereo app recording a surround cable gets Windows' capture downmix: the front channels 13.5 dB quieter, with center, LFE and the surrounds mixed in. Micser captures all channels and folds them itself (`ChannelMixer`), but e.g. Discord or OBS recording the cable in stereo would be affected. Surround is therefore opt-in per cable, and the settings warn about it.
  - Windows doesn't set the endpoint's `PKEY_AudioEndpoint_PhysicalSpeakers`, which some games might read instead of the mix format (untested).
- **DRM.** Render streams with `CopyProtect` rights don't write into the cable.
- **DriverUtility** (Native AOT exe, SetupAPI): `status | install | update | set-count | set-layout | sync-formats | uninstall`. It copies itself and the driver package to `%ProgramFiles%\Micser\Driver` with its own "Apps and Features" entry, so uninstalling Micser leaves the driver, and logs to `%ProgramData%\Micser\logs`. The shell runs it elevated with the engine paused, reads its status at start (a tray notice for a newer bundled driver), and the settings' "Virtual audio cables" section drives it.
- **Build.** The WDK and SDK come from NuGet (`src/Vac/packages.config`, restored by `scripts/build-vac.ps1`); the build also needs the WDK component of Visual Studio (`Microsoft.Windows.DriverKit`) and the Spectre-mitigated libraries, and the 64-bit MSBuild because the WDK packages only ship 64-bit host tools. x64 and ARM64, warnings as errors, Spectre mitigation, InfVerif `/w` after each build. Minimum Windows 10 2004 (19041) because of `ExAllocatePool2`. CI builds it on the `windows-2025-vs2026` image.
- **Signing.** Builds are test-signed with the WDK test certificate. Release signing (EV certificate, attestation signing) is pending.
- **Testing.** `scripts/deploy-vac-vm.ps1` installs a build in the Hyper-V VM "DriverTesting" with test signing on (PowerShell Direct, `devcon`), and with `-TestSeconds` runs `AudioHarness latency` from cable input to cable output there. `AudioHarness formats` checks the accepted formats and the channel mapping.
  - Every dev build has the same `DriverVer`, so PnP keeps using an older package from the driver store and `devcon update` still reports success. The script therefore removes the device and all Micser packages before each install.
  - In an enhanced (RDP) session the VM only shows "Remote Audio", not the cable endpoints. PowerShell Direct and the basic console session see them.
  - A user must be signed in at the console (basic session): otherwise the audio engine renders silence for the PowerShell Direct session's streams, also into the loopback. The VM signs in automatically (Winlogon `AutoAdminLogon`, with `DevicePasswordLessBuildVersion` = 0), so this survives reboots.
  - In Debug builds, a second adapter (e.g. when a removed device still waits for a reboot) hits a breakpoint in `NewAdapterCommon` and bugchecks without a debugger; the script reboots the VM when `devcon remove` asks for it.
  - Kernel debug output can be captured with Sysinternals `dbgviewcli64 -k -v --duration <s> -l <file>` in the VM.
  - Driver Verifier (standard checks) is enabled for `MicserVac.sys` in the VM, so every test runs under it.
- **Static analysis.** `scripts/codeql-vac.ps1` runs Microsoft's CodeQL driver suites (`microsoft/windows-drivers`, the WHCP `mustfix` and `recommended` suites) and fails on findings in the driver's code; CI runs it for x64 in a job of its own. Findings in the WDK headers and `cpp/drivers/init-not-cleared` (PortCls creates the FDO) are excluded.

## Testing

- **.NET:** TUnit on Microsoft.Testing.Platform, with NSubstitute. Audio tests drive `AudioGraph.Process()` directly with synthetic modules; nothing in `tests/` opens real devices. Engine tests use `EngineFactory` (temp config and user plugin directory, token required) with a real audio engine and no devices selected; Main is loaded from `plugins/Main` in the test output.
- **Web:** Vitest, from one root `vitest.config.ts` with two projects. `*.test.ts` runs in Node. `*.test.tsx` and `*.browser.test.ts` (DOM, storage, module imports) run in Vitest browser mode on headless Chromium through Playwright, because Fluent UI and React Flow need real layout. Components are rendered with `vitest-browser-react`. `@micser/web-sdk/testing` has the shared helpers: `TestProviders` (theme, query client, an engine connection that is never started), `createTestQueryClient()` (seeded with `setQueryData` under the generated keys, so hooks don't fetch) and `testModule()`. Tests render in English unless they pass `language` to `TestProviders`.
- **End-to-end:** Playwright Test in `tests/E2E`, Chromium only (the shell's WebView2 is Chromium as well), with the browser locale `en-US`.
  - `globalSetup.ts` builds the engine once (Release in CI), and a plugin package: the engine tests' plugin (module type "Test") with the hand-written widget bundle in `tests/E2E/plugin/web`, which imports `react` through the UI's import map like a real plugin bundle.
  - Each worker starts its own engine from the build output and its own Vite dev server, both on free ports (`servers.ts`, the worker-scoped `servers` fixture), so the tests run in parallel.
    - The engine listens on port 0 and is ready once it has written its discovery file, which has the port. It gets a temporary config and user plugin folder, with no token and no single-instance check.
    - Vite runs through its API with its own dependency cache per worker; concurrent servers would otherwise write the same one. The fixture loads the UI once before the worker's first test, since the first load compiles it.
  - CI runs them against a published engine instead (`MICSER_E2E_PUBLISHED_ENGINE`, the engine's assembly after `npm run build` and `dotnet publish`): it serves the built UI and plugin widgets, as in a release, and the workers start no Vite server. The engine starts in its own folder, where ASP.NET Core finds `wwwroot`. Locally, Vite stays the default, so the tests run without a publish and with React's development checks (`StrictMode`).
  - CI splits the tests into two shards (`--shard`), one job each, because the runner's 4 cores only get 2 workers.
  - A worker's tests share its engine, and each starts from an empty graph: the `engine` fixture deletes subgraphs, modules and templates and restores the default preferences (`EngineApi.reset()`).
    - Tests that change more, e.g. the plugins, get an engine and Vite server of their own (`test.use({ ownEngine: true })`). The `app` fixture's `restartEngine()` stops that engine gracefully (`POST /api/engine/shutdown`) and starts it at the same address, as the shell does.
  - `EngineApi` sets up state and checks results through the HTTP API; `Graph` wraps the React Flow DOM (nodes by `data-id`, ports, connections, menus, notifications).
  - `FakeShell` injects `chrome.webview` before the UI loads: it answers `getState` with a given `ShellState`, records the messages the UI posts and sends the shell's messages, so the shell-only controls are tested without WebView2. The shell's own side of the web messages (`MainForm`) isn't covered.

## Development

- **Orchestration.** `aspire start` (or `aspire run`) runs `tools/AppHost`; `aspire.config.json` at the root points to it.
  - `engine` gets an Aspire-assigned port. The endpoint isn't proxied, because the engine's `Urls` setting would override `ASPNETCORE_URLS`, so the AppHost sets `Urls` itself. Its health check is `/api/health`.
  - `web` is the Vite dev server, with `MICSER_ENGINE_URL` set to the engine's endpoint. Aspire runs `npm install` in `src/Web` first, which installs the workspace at the root.
  - `shell` is started from the dashboard only, with `--ui` pointing to Vite.
  - Logs, traces and metrics reach the dashboard through `Micser.ServiceDefaults`. Serilog keeps its own sinks and forwards to the OpenTelemetry logger provider (`writeToProviders`). Without `OTEL_EXPORTER_OTLP_ENDPOINT` nothing is exported.
  - The manual workflow (`dotnet run` on port 5080 plus `npm run dev`) works without Aspire.
- **npm.** The internal packages (`@micser/web-sdk`, the plugins' widget packages) export TypeScript source (`"exports": "./src/index.ts"`) and have no build step; only the SPA and the widget bundles are built. TypeScript is pinned to `~6.0` until `typescript-eslint` supports 7.x.
- **npm supply chain.** Volta pins the Node version (`volta.node` in `package.json`), and npm refuses to install on a Node outside the `engines` ranges (`engine-strict`). npm installs run no lifecycle scripts (`ignore-scripts`) and skip versions younger than 3 days (`min-release-age`), both in `.npmrc`. `@lavamoat/allow-scripts` keeps the allowlist of packages with install scripts (`lavamoat.allowScripts` in the root's and each workspace's `package.json`, since allow-scripts only follows the dependencies of the package it runs in); `npm run setup` installs and then runs the allowed ones. `@lavamoat/preinstall-always-fail` stays denied, so an install that runs scripts anyway fails. CI caches `node_modules` after `setup:ci` (`.github/actions/npm-install`) and fails when `allow-scripts:auto` changes an allowlist.
- **Commit hook.** `npm run setup` installs Husky's hooks (`.husky`; the `prepare` script doesn't run under `ignore-scripts`). The pre-commit hook runs lint-staged (`lint-staged` in the root `package.json`) one task at a time, since the tasks write the same files: `scripts/repair-line-endings.mts` and Prettier on all staged files, then `eslint --fix` and Prettier again on the scripts. The Node scripts in `scripts/` are linted and type-checked (`scripts/tsconfig.json`) with the rest.
- **Line endings.** `.gitattributes` sets LF for the files Prettier formats and the hooks, CRLF for the INX, and leaves the rest to `core.autocrlf`. Git applies them only when it writes a file itself, and some tools (CodeMaid) write their own line endings; `npm run fix-line-endings` rewrites the working tree to what a checkout would produce (`--dry-run` lists the files).
- **CI** (`.github/workflows/ci.yml`) runs the .NET build and tests, the driver build and CodeQL, the npm format/lint/typecheck/test/build and the end-to-end tests as separate jobs.
