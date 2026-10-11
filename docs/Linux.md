# Linux

Micser runs on Windows. This document maps what it takes to run it on Linux as well, and the plan for doing so. The engine, the audio graph, the plugins, the web UI and all tests are portable (phase 1), and on Linux the engine routes audio through PipeWire (phase 2). The rest is virtual cables, a cross-platform shell and packaging.

Decisions so far: PipeWire's native API for audio, a cross-platform shell (tray + embedded web view) instead of browser-only, and one engine that targets `net10.0` on both platforms.

## Windows dependencies

| Area                                  | Windows                                              | Linux                                                                     | Phase        |
| ------------------------------------- | ---------------------------------------------------- | ------------------------------------------------------------------------- | ------------ |
| Device I/O                            | WASAPI through NAudio (`Devices/Wasapi`)             | PipeWire streams (`pw_stream`)                                            | 2            |
| Device list, hot-plug, default device | `MMDeviceEnumerator`, notification client            | PipeWire registry, `default` metadata                                     | 2            |
| System volume (`useSystemVolume`)     | endpoint volume of the default render device         | `Props` of the default sink node                                          | 2            |
| Resume after sleep                    | `PowerRegisterSuspendResumeNotification`             | stall watchdog; logind `PrepareForSleep` (D-Bus) if needed                | 2            |
| Audio thread                          | MMCSS "Pro Audio", high-resolution waitable timer    | `SCHED_FIFO` (`RLIMIT_RTPRIO`) or RTKit, `clock_nanosleep`                | 1 (RTKit: 2) |
| One engine per session                | named semaphore `Local\Micser.Engine`                | lock file in `$XDG_RUNTIME_DIR/micser`                                    | 1            |
| Discovery file                        | `%LocalAppData%\Micser\engine.json` (private folder) | `$XDG_RUNTIME_DIR/micser/engine.json`, mode 0600                          | 1            |
| Configuration, user plugins, logs     | `%AppData%`, `%LocalAppData%`                        | `~/.config/Micser`, `~/.local/share/Micser` (`Environment.SpecialFolder`) | 1            |
| Virtual cables                        | PortCls kernel driver, `DriverUtility` (elevated)    | PipeWire null-audio-sink nodes, created by the engine                     | 3            |
| Shell                                 | WinForms tray, WebView2, `HKCU\...\Run`, named event | cross-platform shell (see below), XDG autostart                           | 4            |
| Packaging and updates                 | Velopack, `Setup.exe`                                | Velopack AppImage, later Flatpak                                          | 5            |

The graph, the DSP (NAudio's `Dsp` namespace is in the portable `NAudio.Core`), ring buffers, drift control and resampling, the API, the configuration, the plugin loader and the web UI need no changes.

## Phase 1: portable core (done)

- `Micser.Audio`, `Micser.Engine`, `Micser.Plugins.Main` and their tests target `net10.0`. NAudio comes as `NAudio.Core` + `NAudio.Wasapi` (both `net9.0`), not the metapackage. NAudio marks the WASAPI API `[SupportedOSPlatform("windows")]`, so the platform analyzer (CA1416, an error with `TreatWarningsAsErrors`) keeps every WASAPI call behind a Windows check.
- Plugins are platform-neutral assemblies; the same plugin package loads on both.
- **Backends.** `AudioDeviceService` keeps its public API and delegates to an internal `IAudioBackend`: `WasapiBackend` on Windows, `NullAudioBackend` (no devices) elsewhere. A backend opens `ICaptureDevice`/`IRenderDevice` endpoints (layout, sample rate, period; capture pushes interleaved floats, render pulls them). `CaptureStream` and `RenderStream` keep the platform-neutral part: ring buffer, adaptive target, drift control and resampling. `SystemVolume` delegates to the backend's implementation.
- **Audio thread.** `BlockTimer` per platform: MMCSS and the waitable timer on Windows; on Linux `SCHED_FIFO` (priority 20, with `SCHED_RESET_ON_FORK`) if `RLIMIT_RTPRIO` allows it, otherwise a warning, and `clock_nanosleep` on the monotonic clock.
- **Engine hosting.** The discovery file and the lock file go to `$XDG_RUNTIME_DIR/micser` (0700); the discovery file is 0600 (`PrivateFiles`). The log path `%LOCALAPPDATA%` in `appsettings.Production.json` is set by the engine where the OS doesn't set it.
- **Build and CI.** `Micser.slnx` builds on Linux; the WinForms projects set `EnableWindowsTargeting`. CI's `linux` job builds the solution, runs the portable .NET tests, checks the OpenAPI document and runs the end-to-end tests against a published engine. It isn't a release requirement.
- Findings on the way:
  - The OpenAPI document differed by platform: the XML comment generator writes multi-line descriptions with the platform's line breaks. A document transformer now normalizes them to LF (`OpenApiConfiguration`).
  - `.husky/pre-commit` needs the executable bit for Git on Linux (`dotnet husky install` sets it).
  - `dotnet format style` with the Linux SDK reports `JSON002` and `IDE0002` in a few test files that pass on Windows.

## Phase 2: PipeWire backend (done)

`PipeWireBackend` (`src/Audio/Devices/PipeWire`) talks to PipeWire's native API; `AudioDeviceService` uses it on Linux when `libpipewire-0.3.so.0` loads and a daemon answers, and falls back to `NullAudioBackend` otherwise (logged). It reconnects when the daemon restarts (after 1 s, backing off to 10 s); streams of the lost connection fault, and the device modules reopen them when their devices come back.

**Binding** (`PipeWireNative`):

- `LibraryImport` for the exported functions (thread loop, context, streams, properties). The core, registry, node and metadata methods are inline C functions, so they're called through each object's `spa_interface` method table.
- Callbacks are `[UnmanagedCallersOnly]` functions in static event tables, with a `GCHandle` of the managed object as their data. Exceptions don't leave them (they're logged).
- `SpaPod` builds the stream's `EnumFormat` (F32, rate, positions) and reads `Props` (`channelVolumes`, `mute`); tests compare its output with bytes from SPA's own builder. Layouts and constants were checked against the 1.0.5 headers (`libpipewire-0.3-dev`) with a small C program.
- A `pw_thread_loop` runs the events. Its lock guards all calls into PipeWire; device state is in managed objects under a separate lock, which is only taken inside the loop lock.

**Devices:**

- Sinks (`Audio/Sink`) are outputs, sources (`Audio/Source`, `Audio/Source/Virtual`) inputs; PipeWire only has the nodes of active devices and profiles, so there are no inactive ones. `Id` = `node.name` (stable, e.g. `alsa_output.usb-…analog-stereo`), `Name` = `node.description`, `Description` = `device.profile.description`, `AdapterName` = the parent device's `device.description`.
- Each audio node is bound for its info, which has `audio.channels` and `audio.position`; a node is listed (and `Added` raised) once that arrived. `Removed` comes with `global_remove`, `DefaultChanged` with the `default` metadata (`default.audio.sink`/`source`). The sample rate is the graph's (`clock.rate`, or `clock.force-rate`, from the `settings` metadata).
- Layout: positions with a speaker map to a WAVEFORMATEXTENSIBLE mask (`SpaAudioChannels`; `MONO` is mono, `RL`/`RR` are the back speakers, `SL`/`SR` the sides). Streams ask for the positions sorted by speaker bit, so PipeWire's channel converter turns e.g. ALSA's 5.1 order (FL FR RL RR FC LFE) into Micser's. Positions without a speaker (`AUX0`…) or repeated ones are taken channel by channel.

**Streams** (`PipeWireStreamDevice`): one `pw_stream` per device stream, F32 interleaved at the graph rate in the device's channels, so PipeWire neither resamples nor mixes.

- Capture connects to the source (`target.object`), loopback to a sink with `stream.capture.sink`, render to the sink. `node.dont-reconnect`: a stream whose device goes away faults instead of following another device, like a WASAPI stream.
- The `process` callback runs on PipeWire's realtime data thread (`PW_STREAM_FLAG_RT_PROCESS`) and feeds the same ring buffers, drift control and resampling as WASAPI.
- Streams ask for a 512-frame period (`node.latency`, 10.7 ms at 48 kHz, close to WASAPI's 10 ms); PipeWire runs a device at the smallest period its streams ask for, which `StreamBuffering` sizes the targets from.
- Measured: the loopback round trip through a virtual sink is about 37 ms (`AudioHarness latency`); USB devices route without dropouts after the first seconds.

**System volume** (`PipeWireSystemVolume`): the default sink's `Props`, watched through the node's param events. `channelVolumes` are linear (the desktop's volume control shows their cube root), so the loudest channel is the gain, as `useSystemVolume` expects.

**Testing:**

- `SpaPodTests` and `SpaAudioChannelsTests` run everywhere.
- `PipeWireBackendTests` run against `PipeWireTestDaemon`: a private `pipewire` and WirePlumber (on a private D-Bus session from `dbus-run-session`) with their own runtime, configuration and state folders and without device monitors, so they only see virtual sinks and sources and never touch the session's audio. They cover listing, playing to a sink and capturing its monitor, surround channel order, hot-plug with faulting streams, the system volume and a daemon restart. They're skipped where `pipewire`, `wireplumber` 0.4 (Lua configuration), `dbus-run-session` or `pw-cli` are missing; CI's `linux` job installs them.
- `tools/AudioHarness` targets `net10.0`: `list`, routing and `latency` work on both platforms, `formats` (WASAPI formats) on Windows only.

**Open:**

- Realtime priority through RTKit (D-Bus `org.freedesktop.RealtimeKit1.MakeThreadRealtime`) when `RLIMIT_RTPRIO` is 0, the default for users outside PipeWire's or JACK's realtime group; the engine thread then runs at normal priority.
- Resume after sleep: PipeWire suspends and resumes devices itself and the stall watchdog covers streams that stop; logind's `PrepareForSleep` (D-Bus) only if streams turn out to look healthy but be silent, as on Windows.
- WirePlumber 0.5 (SPA-JSON configuration) for the test daemon, for distributions that ship it.
- The engine as a PipeWire filter node, driven by the graph clock instead of its own timer: for PipeWire devices that removes the ring buffers, resampling and drift correction and most of the latency, but changes the engine's clocking model.

## Phase 3: virtual cables

PipeWire creates virtual devices without a kernel driver:

- The engine creates a node per cable side with the `support.null-audio-sink` factory: "Cable N Input" (`Audio/Sink`) and "Cable N Output" (`Audio/Source/Virtual`), linked by the engine or a loopback, with the cable's layout as `audio.position`. Nodes live as long as the engine's connection unless `object.linger` is set; lingering nodes survive engine restarts, which matches the driver's behaviour.
- No elevation, signing or reboot: the settings section talks to a new engine API (`/api/cables`) instead of the shell's `DriverUtility` messages. The UI picks the API on Linux and the shell bridge on Windows (a capability in `GET /api/engine` or the shell state).
- Surround layouts need no device format workaround (`IPolicyConfig`); the node's format is its declared position list.

## Phase 4: cross-platform shell

What the shell must do (see [Shell](Architecture.md#shell)): tray with menu, a window with a web view showing the engine's UI, the web message bridge (`shell.ts`), links to the default browser, no default context menu, color mode and language, autostart, single instance, engine supervision and updates.

Options:

1. **Avalonia 12** (recommended, behind a spike). `TrayIcon` uses StatusNotifierItem over D-Bus on Linux. For the web view: Avalonia's WebView or the MIT [`NativeWebView`](https://www.nuget.org/packages/NativeWebView) package (WebView2 on Windows, WebKitGTK on Linux). One shell for both platforms; the spike checks the web view's bridge, context menu and color scheme APIs, Linux maturity and licensing.
2. **WinForms on Windows plus a GTK shell on Linux** (GirCore: GTK 4 + WebKitGTK 6, SNI tray through Tmds.DBus). Keeps the Windows shell as it is at the cost of two shells.
3. **Photino / Rustino.NET** (WebView2 / WebKitGTK windows). Photino has no tray; its development has slowed down.

Either way:

- `shell.ts` gets a transport interface over `chrome.webview` (WebView2) and the Linux web view's message API, and `FakeShell` keeps testing the UI side.
- WebKitGTK isn't Chromium: the UI needs a Playwright WebKit project (smoke tests at least) to cover the Linux shell's engine.
- GNOME shows no tray icons without the AppIndicator extension (Ubuntu ships it, Fedora doesn't), so everything must also work from the window and the app launcher; closing the window keeps the engine running there as well.
- Autostart: an XDG autostart entry (`~/.config/autostart/micser.desktop`). Single instance: the shell's own lock file plus a D-Bus or socket message to show the first instance's window. Color mode: the `color-scheme` setting of xdg-desktop-portal. Language: `LANG`/`LC_MESSAGES`.
- Engine supervision as on Windows. A systemd user unit (`Restart=on-failure`) would be the native alternative, but supervision through the shell keeps both platforms alike.
- Never pass the token on a command line: `/proc/<pid>/cmdline` is readable by other users. The shell loads `{url}/#token=…` in its own web view; "open in browser" would need a 0600 redirect file in the runtime folder.

## Phase 5: packaging

- **Velopack AppImage** first: the same `vpk` pipeline (`linux-x64`, self-contained), the same GitHub releases and the updater the shell already uses. Check which install hooks run on Linux; otherwise the shell sets up autostart on its first start.
- **Flatpak** (Flathub) later, where Linux desktop apps are expected: it needs `--filesystem=xdg-run/pipewire-0` for PipeWire and `--talk-name=org.kde.StatusNotifierWatcher` for the tray.
- Distribution packages (AUR, .deb) are optional.

## Positioning

PipeWire routes audio between applications and devices by itself, and tools like qpwgraph (graph editing) and EasyEffects (effects) cover parts of what Micser does. Micser's value on Linux is the same combination as on Windows: routing, per-route effects, analyzers, templates and plugins in one graph UI, with the engine running headless. The filter-node option (phase 2) would make it a native citizen of the PipeWire graph rather than a client with its own clock.

## macOS

The same split carries a CoreAudio backend (`AudioObject` listeners for devices, `AudioUnit` HAL output for streams, process taps for loopback on macOS 14.4+) and `thread_policy_set` for the audio thread. The shell options above (Avalonia + WKWebView) cover macOS too. Not planned yet.
