import type { PluginDto } from "@micser/web-sdk";
import { expect, test } from "vitest";
import { loadPluginWidgets } from "./plugins";

function bundle(source: string) {
  return URL.createObjectURL(new Blob([source], { type: "text/javascript" }));
}

function pluginBundle(name: string, moduleTypes: string[]) {
  const widgets = moduleTypes.map((moduleType) => ({ moduleType, title: moduleType }));
  return bundle(`export default ${JSON.stringify({ name, widgets })};`);
}

function plugin(id: string, properties: Partial<PluginDto>): PluginDto {
  return {
    id,
    name: id,
    version: "1.0.0",
    isBuiltIn: false,
    isLoaded: true,
    error: null,
    webUrl: null,
    pendingChange: "None",
    ...properties,
  };
}

test("collects the widgets of the loaded plugins by module type", async () => {
  const { widgets, failures } = await loadPluginWidgets([
    plugin("Main", { webUrl: pluginBundle("Main", ["Gain", "Equalizer"]) }),
    plugin("Extra", { webUrl: pluginBundle("Extra", ["Reverb"]) }),
  ]);

  expect([...widgets.keys()]).toEqual(["Gain", "Equalizer", "Reverb"]);
  expect(widgets.get("Reverb")?.title).toBe("Reverb");
  expect(failures).toEqual([]);
});

test("skips plugins that aren't loaded or have no widgets", async () => {
  const { widgets, failures } = await loadPluginWidgets([
    plugin("Failed", { isLoaded: false, webUrl: pluginBundle("Failed", ["Gain"]) }),
    plugin("Headless", { webUrl: null }),
  ]);

  expect(widgets.size).toBe(0);
  expect(failures).toEqual([]);
});

test("reports a bundle that doesn't export a plugin", async () => {
  const notAPlugin = plugin("Broken", { webUrl: bundle("export default 42;") });

  const { failures } = await loadPluginWidgets([notAPlugin]);

  expect(failures).toHaveLength(1);
  expect(failures[0].plugin).toBe(notAPlugin);
  expect(String(failures[0].error)).toContain("definePlugin");
});

test("reports a bundle that fails to load and keeps the others", async () => {
  const missingUrl = bundle("");
  URL.revokeObjectURL(missingUrl);

  const { widgets, failures } = await loadPluginWidgets([
    plugin("Missing", { webUrl: missingUrl }),
    plugin("Main", { webUrl: pluginBundle("Main", ["Gain"]) }),
  ]);

  expect(failures.map((failure) => failure.plugin.id)).toEqual(["Missing"]);
  expect([...widgets.keys()]).toEqual(["Gain"]);
});
