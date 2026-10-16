# @micser/web-sdk

The SDK for the widgets of [Micser](https://github.com/loreggia/micser) plugins. Micser is an audio router for Windows and Linux; a
plugin adds audio modules to its engine (with the NuGet package [`Micser.Audio`](https://www.nuget.org/packages/Micser.Audio)) and, in
an ES module bundle, the widgets that edit them in the web UI.

- `definePlugin` and `defineWidget`: the bundle's default export.
- Controls (`ParameterKnob`, `FrequencyResponse`, `InfoTip`, ...), labels and number formats, and the engine hooks (`useModuleUpdate`,
  `useModuleData`, ...).
- `defineTranslations`: the plugin's texts, in the UI's i18next instance.
- `@micser/web-sdk/vite`: `definePluginBuild()`, the Vite configuration of the bundle.
- `@micser/web-sdk/testing`: `TestProviders` for widget tests.

## Usage

```ts
// src/index.ts
import { definePlugin, defineWidget } from "@micser/web-sdk";
import { MyWidget } from "./MyWidget";

export default definePlugin({
  name: "MyPlugin",
  widgets: [defineWidget({ moduleType: "MyModule", title: "My module", component: MyWidget })],
});
```

```ts
// vite.config.ts
import { definePluginBuild } from "@micser/web-sdk/vite";
import { defineConfig } from "vite";

export default defineConfig(definePluginBuild());
```

`vite build` writes `dist/index.js`, which goes into the plugin's folder (`"web": "web/index.js"` in its `plugin.json`). The bundle
doesn't include React, Fluent UI, TanStack Query and this package: it uses the UI's instances. The package version is the Micser
version it was released with.

The type declarations need `"moduleResolution": "Bundler"` in `tsconfig.json`. The built-in plugin
[`src/Plugins/Main`](https://github.com/loreggia/micser/tree/main/src/Plugins/Main) is the reference, and
[docs/Architecture.md](https://github.com/loreggia/micser/blob/main/docs/Architecture.md#plugins) describes plugins.
