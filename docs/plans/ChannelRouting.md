# Plan: per-channel routing

Route single channels of a port to other modules, e.g. for audio interfaces that expose all their channels on one device. Similar to Loopback 2 on macOS. Only the internal routing changes; the VAC driver stays as it is.

## Behaviour

- Each module has "Show channels" in its "More" menu. Off, the module looks and behaves as now. On, each port shows its combined connector (all channels, as now) and below it one connector per channel.
- "Show channels" can only be turned off while none of the module's channel connectors are in use.
- Modules whose inputs take their layout from their sources (effects, spectrum) get a "Channels" setting: Auto (as now) or a fixed channel count (1–64), so an input offers a known set of channel connectors even before anything is connected.

## Model

Channels are part of a connection, not separate ports. Module types, plugins, `ModuleTypeDto`'s port names and widgets stay as they are; per-channel connectors are a view of a port.

- A connection gets `sourceChannel` and `targetChannel` (0-based, optional). Without both it carries the whole port, as now.

  | Connection        | Mixing                                                                               |
  | ----------------- | ------------------------------------------------------------------------------------ |
  | whole → whole     | as now (`ChannelMixer`)                                                              |
  | channel → whole   | the source channel as mono, by the mono rule (front center, or front left and right) |
  | whole → channel   | the source folded to mono (average without LFE), added to that channel               |
  | channel → channel | added 1:1                                                                            |

- A channel that the current layout doesn't have is silent. Such connections are kept (config, API), so they work again when e.g. an unplugged device comes back.
- `ModuleDto` gets `showChannels` (default false) and `channelCount` (null = Auto, otherwise 1–64). Both are saved with the module and in templates.
- `ModuleTypeDto` gets `supportsChannelCount`: the module has an input whose layout comes from its sources.

## Phase 1: audio and engine (done)

- `src/Audio`
  - `Connection` gets `SourceChannel` and `TargetChannel`. `AudioGraph.Connect` treats ports plus channels as unique, so a whole and a channel connection between the same ports can both exist. Cycle detection stays per module.
  - `InputPort.Mix` uses precomputed routes per (source layout, channel selection, target layout), cached like the mixers now: created only when a layout changes, no allocation on the normal path.
  - Input ports know whether their module fixed the layout (device outputs) or left it to the sources. For the latter:
    - with a channel count, the layout is `ChannelLayout.FromChannelCount` (speaker positions for 1, 2, 4, 6 and 8 channels, by index otherwise);
    - on Auto, it's the widest layout of the whole-port sources, widened to at least the highest target channel + 1 and at least stereo when only channel connections exist.
  - An effect's output copies its input's layout, so it follows without further changes.
- API and `AudioHost`
  - `ConnectionDto` and `CreateConnectionRequest` get `int? sourceChannel` and `int? targetChannel` (`[Range(0, 63)]`). The duplicate check (409) includes them. Channels the port doesn't have right now are accepted, since layouts change at runtime.
  - A module with channel connections on its side has `showChannels = true`: the end that names a channel counts (for channel → whole only the source module, for channel → channel both). Creating a channel connection (API or template instantiation) turns it on and broadcasts `ModuleChanged`; turning it off while such connections exist is rejected (400).
  - A `channelCount` below the highest target channel of the module's channel connections is rejected (400), and so is a connection to a channel beyond a set `channelCount` of an input without a fixed layout. `[Range(1, 64)]`.
  - Restoring the configuration and instantiating or updating from a template fix `showChannels` and `channelCount` instead of rejecting, so nothing is dropped.
  - "Update from template" takes the template's `showChannels` and `channelCount` unless that would break these rules.
- Config and templates
  - `TemplateConnectionDto` gets the channels; updating from a template matches on them.
  - The config stays at `Version = 1` with null defaults: the store moves a file with another version to `.bak` and starts empty, so a bump would lose the user's setup on a downgrade. An older engine ignores the new fields and turns channel connections into whole ones.
- Tests: the four mixing cases, out-of-range channels, Auto widening, fixed channel counts, duplicates; API validation, the `showChannels` and `channelCount` rules, config round trip, templates.

## Phase 2: port layouts (done)

- Each port publishes its last layout (channel count and speaker mask) atomically from the audio thread, in one 64-bit write.
- `GET /api/port-layouts` returns all of them; the hub pushes changes as `PortLayoutsChanged`, checked in the same loop that reads the levels. A device module without an open stream reports zero channels. They're runtime state, not part of `ModuleDto` or the config.
- Regenerate the API client. The web SDK patches the cached layouts from `PortLayoutsChanged` (and drops a removed module's); `usePortLayouts(moduleId)` reads them.

## Phase 3: UI (done)

- The "More" menu gets "Show channels" (checked item, disabled with a tooltip while channel connections exist) and, for modules with `supportsChannelCount`, a "Channels" submenu: Auto, Mono, Stereo, Quad, 5.1, 7.1 and "Custom…" (number field, 1–64). Counts below what the channel connections need are disabled.
- Ports: with channels shown, each port has its combined connector and a row per channel, labelled from the layout ("1 (L)", "2 (R)", "3"). The rows sit in a fixed row grid instead of being spread over the card's edge, with a minimum node height; collapsed modules keep them too.
- Handle ids become `port` and `port:ch`; the collapsed subgraph's proxy ids (`proxyHandleId`, `resolveHandle`) get an optional `:ch`, with channel rows only for members that show channels.
- Connections to a channel the port doesn't have right now are dashed.
- Rerouting a connection's end and dropping a connection on empty space keep the channel; a new module from a channel connector is connected with its whole first input.
- Texts in `en.ts` and `de.ts`. Vitest: handle id parsing, `ModuleNode` with channel rows (layouts seeded into the query client), the menu items' disabled states.

## Phase 4: finish

- End-to-end: turn on "Show channels", connect a channel, check that the item is disabled, set a channel count, delete the connection, turn it off; each step checked through `EngineApi`.
- `docs/Architecture.md` (Audio engine: graph, buffers and layouts; Engine: API, hub, configuration, templates; UI: widgets, graph editor) and "Unreleased" in `CHANGELOG.md`. Remove this plan once it's described there.
- Optional: per-channel meter bars aligned with the channel rows.
