import { isPlugin, localize, type Plugin, type PluginDto, type WidgetDefinition } from "@micser/web-sdk";
import { createContext, useContext } from "react";

export interface PluginWidgets {
  /** The widgets of all loaded plugins by module type. */
  widgets: ReadonlyMap<string, WidgetDefinition>;
  /** Whether the plugins' widget bundles are still loading. */
  isLoading: boolean;
}

export const PluginWidgetsContext = createContext<PluginWidgets>({ widgets: new Map(), isLoading: true });

export function usePluginWidgets() {
  return useContext(PluginWidgetsContext);
}

/** The label of a module's port in the current language: the widget's, or the engine's port name. */
export function portName(widget: WidgetDefinition | undefined, port: string) {
  const label = widget?.portLabels?.[port];
  return label ? localize(label) : port;
}

/** The widget bundles by URL. The browser loads a module once, so a changed bundle takes a reload. */
const bundles = new Map<string, Promise<Plugin>>();

function importBundle(url: string) {
  let bundle = bundles.get(url);
  if (!bundle) {
    bundle = import(/* @vite-ignore */ url).then((module: { default?: unknown }) => {
      if (!isPlugin(module.default)) {
        throw new Error("The widget bundle doesn't default-export a plugin (definePlugin).");
      }

      return module.default;
    });
    bundles.set(url, bundle);
  }

  return bundle;
}

export interface PluginLoadFailure {
  plugin: PluginDto;
  error: unknown;
}

/**
 * Imports the widget bundles of the loaded plugins (see `PluginDto.webUrl`).
 */
export async function loadPluginWidgets(plugins: PluginDto[]) {
  const withWidgets = plugins.filter((plugin) => plugin.isLoaded && plugin.webUrl);
  const results = await Promise.allSettled(withWidgets.map((plugin) => importBundle(plugin.webUrl!)));

  const widgets = new Map<string, WidgetDefinition>();
  const failures: PluginLoadFailure[] = [];
  results.forEach((result, index) => {
    if (result.status === "fulfilled") {
      result.value.widgets.forEach((widget) => widgets.set(widget.moduleType, widget));
    } else {
      failures.push({ plugin: withWidgets[index], error: result.reason });
    }
  });

  return { widgets, failures };
}
