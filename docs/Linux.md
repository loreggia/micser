# Linux

Micser runs on Windows. This document maps what it takes to run it on Linux as well, and the plan for doing so. The engine, the audio graph, the plugins, the web UI and all tests are portable already (phase 1, done): on Linux the engine runs with no audio devices. The rest is a PipeWire audio backend, virtual cables, a cross-platform shell and packaging.

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

## Phase 2: PipeWire backend

**Binding.** A thin `LibraryImport` layer over `libpipewire-0.3`, in the style of the existing interop:

- `pw_thread_loop` for the main loop. `pw_stream` per device stream, with callbacks (`pw_stream_events`, `pw_registry_events`) as `[UnmanagedCallersOnly]` function pointers, so the audio path doesn't allocate.
- The SPA POD helpers are inline C functions and macros, not exported. A small C# POD builder and parser covers what's needed: `EnumFormat` for raw F32 audio with channel positions, `Props` (volumes, mute).
- [PipeWire.NET](https://www.nuget.org/packages/PipeWire.NET/) (alpha, bindings generated by ClangSharp) is a reference for the struct layouts, not a dependency.
- The backend loads only if `libpipewire-0.3.so.0` is present and a connection succeeds; otherwise the engine falls back to `NullAudioBackend` and logs why.

**Mapping to `IAudioBackend`:**

- Devices are nodes with `media.class` `Audio/Sink` (outputs) or `Audio/Source` (inputs), from the registry. `Id` = `node.name` (stable, e.g. `alsa_output.usb-…analog-stereo`), `Name` = `node.description`, `AdapterName` = the parent device's `device.description` (via `device.id`), which lets device modules switch ports as they do on Windows. Layout and rate come from `audio.channels`/`audio.position` and the format.
- `DeviceChanged`: registry `global`/`global_remove` and changes of the `default` metadata (`default.audio.sink`, `default.audio.source`, JSON `{"name": …}`).
- Capture: a stream connected to the source (`target.object`). Loopback: a capture stream on a sink with `stream.capture.sink = true`. Render: an output stream to the sink. Streams request F32 interleaved in the device's channel positions, so `SampleConverter` isn't needed.
- `PeriodSeconds`: the graph quantum (`clock.quantum`, 1024 frames at 48 kHz by default; a stream can ask for less with `node.latency`). `StreamBuffering` sizes the targets from it as from WASAPI's device period.
- System volume: `channelVolumes` and `mute` of the default sink node's `Props` (linear gains), watched through the node's param events. With a hardware mixer the volume lives in the device's `Route` param; to verify against what the desktop's volume control shows.
- Resume: PipeWire suspends and resumes devices itself; the stall watchdog covers streams that stop. logind's `PrepareForSleep` (needs a D-Bus client such as Tmds.DBus.Protocol) only if testing shows streams that look healthy but are silent, as on Windows.
- RTKit (`org.freedesktop.RealtimeKit1.MakeThreadRealtime`, same D-Bus client) for the engine thread when `RLIMIT_RTPRIO` is 0, which is the default for users outside PipeWire's or JACK's realtime group.

**Later option:** the engine as a PipeWire filter node, driven by the graph clock instead of its own timer. For PipeWire devices that removes the ring buffers, resampling and drift correction and most of the latency. It changes the engine's clocking model, so it comes after a working stream backend.

**Testing:** integration tests against a headless `pipewire` + `wireplumber` with null sinks (CI service or container), opt-in like the driver's VM tests; still no real devices in `tests/`. `tools/AudioHarness` becomes portable (`list`, routing and `latency` through `AudioDeviceService`; `formats` stays WASAPI-only).

**UI text:** "System volume" texts mention Windows; they become platform-neutral.

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
