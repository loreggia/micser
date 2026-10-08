# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

Micser is a Windows audio router: a headless engine runs a graph of audio modules, a web UI edits it, and a tray shell hosts the UI. `docs/Architecture.md` describes how it is built (components, layout, dependencies, audio engine, API, UI, plugins, shell, packaging, VAC driver, testing). Read the relevant section before structural changes, and keep it up to date when the architecture changes. This file only holds commands and working rules.

## Commands

```sh
dotnet build Micser.slnx
dotnet test --solution Micser.slnx                      # TUnit on Microsoft.Testing.Platform (global.json "test.runner")
dotnet test --project tests/Engine/Micser.Engine.Tests.csproj --treenode-filter "/*/*/HealthEndpointTests/*"

npm run setup                # root; installs all workspaces, runs the allowed install scripts and installs the pre-commit hook
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
npm run fix-line-endings     # rewrites working-tree line endings to what git would check out (.gitattributes, core.autocrlf)
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

## Working preferences

- Don't commit unless explicitly told to. Leave changes uncommitted so the user can review them first.
- Keep code comments short and concise. Describe how the code is, not how it was or why it changed. The exception is when leaving out the history would set a trap for future changes. Comments about concrete future plans are fine.

## Rules

General:

- Keep the folder layout and dependency direction in "Layout" of `docs/Architecture.md`: new projects go into `src/` (shipping), `tests/` (mirroring `src/`) or `tools/` (dev-only).
- Nothing in `src/` compiles against a plugin, Main included (see "Plugins").
- Package versions are central in `Directory.Packages.props`, so `PackageReference` items carry no `Version`. `TreatWarningsAsErrors` is on for all projects.
- Stick to the libraries in "Overview": no Newtonsoft, EF Core, Prism/Unity, xUnit or Moq.
- npm runs no install scripts; after dependency changes, run `npm run allow-scripts:auto` (CI fails if it changes an allowlist). See "Development" for the npm setup.

C#:

- C# code is cleaned up with CodeMaid (settings in `CodeMaid.config`). Write new code in its layout so a cleanup run doesn't reshuffle it:
  - Member order by type: fields, constructors, destructors, delegates, events, properties, indexers, methods, nested enums, interfaces, structs, classes.
  - Within a type group: by access level (public, internal, protected, private), then alphabetically.
  - A blank line before and after single-line properties.
  - Comments wrap at 150 columns.
- No primary constructors on classes or structs: use explicit constructors that assign `_camelCase` fields or properties. Positional records are fine.

Audio (`src/Audio`, see "Audio engine"):

- `Process` methods run on the audio thread. Don't allocate, lock (except the existing per-block locks) or log there on the normal path.
- Module parameters are plain properties written from other threads. A parameter set that must change atomically is replaced as a whole (see `EqualizerModule.Bands`).
- Nothing in `tests/` opens real devices.

Engine (`src/Engine`, see "Engine"):

- A new module type needs its own state record (data annotations on the record's parameters, as in ASP.NET Core), `IStatefulModule<TState>`, and a registration in its plugin's `ConfigureServices`. The engine, API, config file and OpenAPI pick it up from there.
- Graph changes go through `AudioHost`.
- When changing a plugin's built-in template (`templates.json`), increase its `revision` and keep its id and its modules' ids.

Web UI (see "UI"):

- Never edit `src/WebSdk/openapi/engine.json` or `src/WebSdk/src/api/generated` by hand; regenerate them and commit both.
- Engine data comes from the generated query hooks; don't poll or invalidate after mutations. Module edits go through `useModuleUpdate`.
- Fluent UI v9 components and `makeStyles` with `tokens`; no hard-coded colors. Icons come from `@fluentui/react-icons/svg/<name>`, not the package index (a lint rule).
- SignalR `hub.on` handlers must not return a value; SignalR would send it to the server as an invocation result.
- User-facing text goes through the namespace's `useTranslation()` (`src/Web/src/i18n.ts`, a plugin's `Web/src/i18n.ts`), with the key in both `locales/en.ts` and `locales/de.ts`; numbers through `formatNumber` or the SDK's labels. Widget titles are functions (`() => t(...)`). Text from the engine isn't translated. The shell's texts are in `src/Shell/Strings.resx` and `Strings.de.resx`.
- UI preferences go through `usePreferences`, never browser storage.
- Connector names come from the engine's module definitions, never hard-coded in widgets.
- The modules a plugin bundle shares with the UI (`sharedModules`) must stay in sync with `definePluginBuild()`'s externals.
- Prettier style: 2 spaces, double quotes, semicolons, print width 120.

Tests (see "Testing"):

- Web tests sit in a `__tests__` folder next to the code they test. Render widgets inside `TestProviders` and seed the query client instead of fetching. A dependency that a browser test newly imports goes into `optimizeDeps.include` in `vitest.config.ts`: a second optimizer pass reloads the tests and can load React twice.
- End-to-end tests set up state through `EngineApi` and check results there, not only in the DOM. A test that changes what `reset()` doesn't undo (plugins) uses `test.use({ ownEngine: true })`. Shell-only controls are tested with `FakeShell`.

Driver (`src/Vac`, see "VAC driver"):

- It isn't in `Micser.slnx`; build it with `eng/build-vac.ps1`. Its `Directory.Build.props` replaces the root one.
- Code that runs at DISPATCH_LEVEL (stream position updates, `CCable`) stays in `#pragma code_seg()` and touches only nonpaged memory.
- A change to a cable's format also needs the endpoints' stored device format set again (`DriverUtility`, `CableEndpoints`).
- Keep the INX ASCII: as UTF-16 without BOM, inf2cat didn't recognize the stamped INF.
- Test it in the VM, never on the dev machine.
