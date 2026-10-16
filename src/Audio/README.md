# Micser.Audio

The plugin SDK of [Micser](https://github.com/loreggia/micser), an audio router for Windows and Linux. A plugin adds audio modules to
the engine; this package has the types a plugin's assembly builds against:

- `IAudioPlugin`: the plugin's entry point, which registers its module types in `ConfigureServices` (`AddAudioModule<TModule, TState>`).
- `AudioModule` (`EffectModule` for one input and one output): a module with ports, whose `Process` runs on the audio thread.
- `IStatefulModule<TState>`: a module's parameters as a state record with data annotations, which the engine's API and configuration use.

## Usage

The engine provides `Micser.Audio` and its dependencies at runtime, so a plugin doesn't ship them:

```xml
<PackageReference Include="Micser.Audio" Version="x.y.z" ExcludeAssets="runtime" />
```

The package version is the Micser version it was released with; build against the oldest Micser version the plugin supports. A plugin
targets `net10.0`, so it loads on every platform the engine runs on.

A plugin is a folder with a `plugin.json`, the assembly and, optionally, a widget bundle built with
[`@micser/web-sdk`](https://www.npmjs.com/package/@micser/web-sdk). The built-in plugin
[`src/Plugins/Main`](https://github.com/loreggia/micser/tree/main/src/Plugins/Main) is the reference, and
[docs/Architecture.md](https://github.com/loreggia/micser/blob/main/docs/Architecture.md#plugins) describes packages and loading.
