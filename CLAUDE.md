# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project state

Micser (a Windows audio router) is being rebuilt on `main`, starting from `9386ea4`, the last compiling commit of the old WPF version. `docs/Architecture.md` holds the decisions (process model, UI, IPC, storage, audio model, layout, libraries) and the roadmap; read it before structural changes and keep it updated when a decision changes. The old WPF code (at `9386ea4`) and the local-only `dev` branch are reference material only. Porting code from them means rewriting it to the new architecture, not merging.

## Commands

```sh
dotnet build Micser.slnx
dotnet test --solution Micser.slnx                      # TUnit on Microsoft.Testing.Platform (global.json "test.runner")
dotnet test --project tests/Engine/Micser.Engine.Tests.csproj --treenode-filter "/*/*/HealthEndpointTests/*"

npm run setup                # root; installs all workspaces, then runs the allowed install scripts
npm run allow-scripts:auto   # after dependency changes: adds new packages with install scripts to the allowlists (denied)
npm run build                # typecheck + vite build of src/Web and the plugins' widget bundles (src/Plugins/*/Web/dist)
npm run typecheck            # tsc in every workspace
npx playwright install chromium   # once, for the Vitest browser tests
npm test                     # Vitest: unit project (Node) and browser project (headless Chromium); "npm run test:watch" to watch
npx vitest run src/Plugins/Main/Web/src/widgets/__tests__/GainWidget.test.tsx
npm run test:e2e             # Playwright (tests/E2E): builds the engine, then each worker runs its own engine and Vite on free ports
npm run test:e2e -- specs/subgraphs.spec.ts --headed
# as in CI: against a published engine, which serves the built UI (no Vite)
npm run build && dotnet publish src/Engine -c Release -o artifacts/e2e-engine
MICSER_E2E_PUBLISHED_ENGINE=artifacts/e2e-engine/Micser.Engine.dll npm run test:e2e
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
dotnet run --project tools/AudioHarness -- formats 2 1             # formats output 2 and input 1 accept, then -20 dB tones from 2 to 1, per channel too (run in the VM)
```

```sh
./eng/build-vac.ps1 -Platform x64,ARM64           # VAC driver: restores the WDK NuGet packages, builds src/Vac, runs InfVerif
./eng/deploy-vac-vm.ps1 -TestSeconds 600          # installs the test-signed driver in the Hyper-V VM "DriverTesting" and runs the cable latency test
./eng/codeql-vac.ps1                              # Microsoft CodeQL driver checks (WHCP suites); fails on findings in src/Vac
```

```sh
./eng/pack.ps1 -Version 0.1.0       # Velopack release (Setup.exe, packages) in artifacts/releases; tags vX.Y.Z publish via .github/workflows/release.yml
                                    # release notes: the "## 0.1.0" section of CHANGELOG.md (rename "Unreleased" before tagging)
```

The harness opens real devices: use a very low gain (as above) unless audible output is intended.

CI (`.github/workflows/ci.yml`) runs the dotnet build/test, the driver build, npm format/lint/typecheck/test/build and the end-to-end tests as separate jobs.

## Working preferences

- Don't commit unless explicitly told to. Leave changes uncommitted so the user can review them first.
- Keep code comments short and concise. Describe how the code is, not how it was or why it changed. The exception is when leaving out the history would set a trap for future changes. Comments about concrete future plans are fine.

## Layout and conventions

- `src/` holds everything that ships, grouped by area with short folder names, e.g. `src/Audio/Micser.Audio.csproj`. `tests/` mirrors `src/`, e.g. `tests/Engine/Micser.Engine.Tests.csproj`. `tools/` holds dev-only programs.
- A plugin is one folder containing both halves: `src/Plugins/Main/Micser.Plugins.Main.csproj` with its `plugin.json`, plus its widget package `src/Plugins/Main/Web` (`@micser/plugin-main`). `Directory.Build.props` excludes `Web/**` and `node_modules/**` from .NET item globs.
- Plugins are loaded at runtime (see "Plugins" in `docs/Architecture.md`), Main included: nothing in `src/` compiles against a plugin. A plugin assembly has one public `IAudioPlugin`; its project references `Micser.Audio` with `Private="false"`. Its widget package default-exports `definePlugin({ ... })` and builds with `definePluginBuild()` from `@micser/web-sdk/vite`; the modules shared with the UI (`sharedModules` there) must stay in sync with what plugins may import from the host.
- npm workspaces (root `package.json`): `src/Web` (Vite SPA), `src/WebSdk` (widget contract and shared code), `src/Plugins/*/Web`. The internal packages export TypeScript source (`"exports": "./src/index.ts"`) and have no build step.
- Widgets are matched to engine modules by module type name. Connector names come from the engine's module definitions, never hard-coded in widgets.
- Web UI (see "UI" in `docs/Architecture.md`):
  - Engine API changes flow through `src/WebSdk/openapi/engine.json` (written on engine build) and the generated Orval client in `src/WebSdk/src/api/generated`. Never edit them by hand, and commit both.
  - Engine data comes from the generated query hooks; `EngineConnection` keeps the cache in sync via SignalR, so don't poll or invalidate after mutations. Module edits go through `useModuleUpdate`.
  - Fluent UI v9 components and `makeStyles` with `tokens`; no hard-coded colors. Icons come from `@fluentui/react-icons/svg/<name>`, not the package index (a lint rule): in development, Vite would load all icons (17 MB) on every page load.
  - SignalR `hub.on` handlers must not return a value; SignalR would send it to the server as an invocation result.
  - User-facing text goes through the namespace's `useTranslation()` (`src/Web/src/i18n.ts`, a plugin's `Web/src/i18n.ts`), with the key in both `locales/en.ts` and `locales/de.ts`; numbers through `formatNumber` or the SDK's labels. Widget titles are functions (`() => t(...)`), resolved with `localize()`. Text from the engine isn't translated. The shell's texts are in `src/Shell/Strings.resx` and `Strings.de.resx`.
  - UI preferences are stored by the engine (`/api/preferences`, `usePreferences`), not in browser storage: the UI's origin changes with the engine's random port.
  - The UI talks to the desktop shell through WebView2 web messages (`src/Web/src/shell.ts` ↔ `MainForm`); shell-only controls are hidden in a plain browser.
  - Tests sit in a `__tests__` folder next to the code they test. `*.test.ts` runs in Node. `*.test.tsx` and `*.browser.test.ts` (DOM, storage, module imports) run in Vitest browser mode (Chromium), with `render` from `vitest-browser-react` and locators/`expect.element` from `vitest/browser`. Render widgets inside `TestProviders` from `@micser/web-sdk/testing` and seed the query client instead of fetching. A dependency that a browser test newly imports goes into `optimizeDeps.include` in `vitest.config.ts`: a second optimizer pass reloads the tests and can load React twice.
  - End-to-end tests (`tests/E2E`, Playwright Test) drive the UI against a real engine. Each test starts from an empty engine (`EngineApi.reset()` in `fixtures.ts`); set up state through `EngineApi` and check results there, not only in the DOM. Tests run in parallel; each worker has its own engine and Vite server (`servers` fixture), and its tests run one after another on them. A test that changes what `reset()` doesn't undo (plugins) uses `test.use({ ownEngine: true })`. Shell-only controls are tested with `FakeShell`, which stands in for WebView2's `chrome.webview`.
- Dependency direction:
  - .NET: `Plugins → Audio` and `Engine → Audio, ServiceDefaults`. The engine's reference to a built-in plugin is build-only (`ReferenceOutputAssembly="false"`, `OutputItemType="BuiltInPlugin"`), which copies it to `plugins/<id>` (`src/Engine/BuiltInPlugins.targets`). `tools/AppHost` references the runnable projects. `Shell` references no Micser project and talks to the engine over HTTP only.
  - npm: `plugin-* → web-sdk` and `web → web-sdk`. The SPA imports plugin widgets at runtime from the URLs the engine reports.
- Node is pinned for Volta (`volta.node` in the root `package.json`); `engine-strict=true` in `.npmrc` makes npm refuse to install on a Node outside `engines.node` (or a package's own `engines`). CI uses the latest Node 24.
- npm runs no install scripts (`.npmrc`: `ignore-scripts=true`) and resolves only versions published at least 3 days ago (`min-release-age=3`). Packages with install scripts are listed with their version in `lavamoat.allowScripts`, in the root `package.json` and in each workspace's (allow-scripts only sees the dependencies of the `package.json` it runs in, so the `allow-scripts:*` scripts run it at the root and in every workspace). `npm run setup` (`setup:ci` in CI) runs the allowed ones (esbuild) after installing. CI caches `node_modules` after `setup:ci` (`.github/actions/npm-install`), keyed on the lockfile, `.npmrc` and the `package.json` files. `@lavamoat/preinstall-always-fail` stays denied: it fails any install that runs scripts anyway. CI fails when `allow-scripts:auto` changes an allowlist.
- Package versions are central in `Directory.Packages.props`, so `PackageReference` items carry no `Version`. `TreatWarningsAsErrors` is on for all projects.
- Libraries:
  - Serilog via `Microsoft.Extensions.Logging` (configured from `appsettings*.json`; the file sink is only enabled in Production).
  - `System.Text.Json`, and TUnit + NSubstitute for tests. Vitest (browser mode with Playwright's Chromium) for the web tests.
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
  - A plugin's templates (`templates.json`) are built-in templates (see "Subgraph templates" in `docs/Architecture.md`). Increase a template's `revision` with every change, so its subgraphs show as outdated, and keep its id and its modules' ids, which the subgraphs match.
  - `AudioHost` is the single entry point for graph changes. It persists and broadcasts every change, and throws `EngineRequestException` for problem responses.
  - Engine tests use `EngineFactory` (temp config and user plugin directory, token required) with a real audio engine and no devices selected. Main is loaded from `plugins/Main` in the test output; `tests/Engine/TestPlugin` is a plugin for the loader tests that the tests don't reference.
- Driver code (`src/Vac`, see "VAC driver" in `docs/Architecture.md`):
  - It isn't in `Micser.slnx`; build it with `eng/build-vac.ps1`. Its `Directory.Build.props` replaces the root one.
  - Code that runs at DISPATCH_LEVEL (stream position updates, `CCable`) stays in `#pragma code_seg()` and touches only nonpaged memory.
  - Cable settings (`CableCount`, `Cable<N>Channels`) live in the device's hardware key and apply at the next device start. A change to a cable's format also needs the endpoints' stored device format set again (`DriverUtility`, `CableEndpoints`).
  - Keep the INX ASCII: as UTF-16 without BOM, inf2cat didn't recognize the stamped INF.
  - Test it in the VM, never on the dev machine.
- The old WaveCyclic driver (`src/Driver`) and the WixSharp installer (driver install custom actions) are in git history before their removal.
