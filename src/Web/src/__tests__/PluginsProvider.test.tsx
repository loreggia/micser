import { Toaster } from "@fluentui/react-components";
import { getGetPluginsQueryKey, type PluginDto } from "@micser/web-sdk";
import { createTestQueryClient, TestProviders } from "@micser/web-sdk/testing";
import { expect, test } from "vitest";
import { page } from "vitest/browser";
import { render } from "vitest-browser-react";
import { toasterId } from "../notifications";
import { usePluginWidgets } from "../plugins";
import { PluginsProvider } from "../PluginsProvider";

function bundle(source: string) {
  return URL.createObjectURL(new Blob([source], { type: "text/javascript" }));
}

function plugin(id: string, webUrl: string): PluginDto {
  return {
    id,
    name: `${id} plugin`,
    version: "1.0.0",
    isBuiltIn: false,
    isLoaded: true,
    error: null,
    webUrl,
    pendingChange: "None",
  };
}

const main = plugin(
  "Main",
  bundle(`export default { name: "Main", widgets: [{ moduleType: "Gain", title: "Gain" }] };`)
);
const extra = plugin(
  "Extra",
  bundle(`export default { name: "Extra", widgets: [{ moduleType: "Reverb", title: "Reverb" }] };`)
);
const broken = plugin("Broken", bundle("export default 42;"));

function Widgets() {
  const { widgets, isLoading } = usePluginWidgets();
  return <div>{isLoading ? "Loading" : `Widgets: ${[...widgets.keys()].join(", ")}`}</div>;
}

async function renderProvider(plugins?: PluginDto[]) {
  const queryClient = createTestQueryClient();
  if (plugins) {
    queryClient.setQueryData(getGetPluginsQueryKey(), plugins);
  }

  const screen = await render(
    <TestProviders queryClient={queryClient}>
      <PluginsProvider>
        <Widgets />
      </PluginsProvider>
      <Toaster toasterId={toasterId} />
    </TestProviders>
  );

  return { screen, queryClient };
}

test("is loading until the plugins are known", async () => {
  const { screen } = await renderProvider();

  await expect.element(screen.getByText("Loading")).toBeVisible();
});

test("provides the widgets of the plugins", async () => {
  const { screen } = await renderProvider([main, extra]);

  await expect.element(screen.getByText("Widgets: Gain, Reverb")).toBeVisible();
});

test("notifies once about a plugin whose widgets fail to load, and shows the others", async () => {
  const { screen, queryClient } = await renderProvider([broken, main]);

  await expect.element(screen.getByText("Widgets: Gain")).toBeVisible();
  const notification = page.getByText("Loading the widgets of the plugin Broken plugin failed");
  await expect.element(notification).toBeVisible();

  // the engine restarted with a new plugin
  queryClient.setQueryData(getGetPluginsQueryKey(), [broken, main, extra]);

  await expect.element(screen.getByText("Widgets: Gain, Reverb")).toBeVisible();
  expect(notification.elements()).toHaveLength(1);
});
