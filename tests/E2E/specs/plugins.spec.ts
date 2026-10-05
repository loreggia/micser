import type { Locator } from "@playwright/test";
import { developmentShell, expect, FakeShell, test, type Graph } from "../fixtures";

// installing changes the engine's plugin folder, which EngineApi.reset() doesn't undo
test.use({ ownEngine: true });

const packagePath = () => process.env.MICSER_E2E_PLUGIN_PACKAGE!;

async function openSettings(graph: Graph) {
  await graph.page.getByRole("button", { name: "Settings" }).click();
  return graph.page.getByRole("dialog", { name: "Settings" });
}

/** The row of a plugin in the settings: its name, version and status. */
function pluginRow(dialog: Locator, name: string) {
  return dialog.getByText(name, { exact: true }).locator("xpath=../..");
}

test("installs a plugin package, which loads with its widget after a restart", async ({ graph, engine, app }) => {
  await graph.open();
  let dialog = await openSettings(graph);

  await dialog.locator("input[type=file]").setInputFiles(packagePath());

  await expect(pluginRow(dialog, "Test plugin")).toContainText("Installed when the engine restarts");
  // without the shell, the engine is restarted by hand
  await expect(dialog.getByText("Restart the engine to apply the changes.")).toBeVisible();
  expect((await engine.plugins()).find((plugin) => plugin.id === "Test")?.pendingChange).toBe("Install");

  await app.restartEngine();

  // the UI reconnects and loads the new plugin's widgets
  await expect(pluginRow(dialog, "Test plugin")).toContainText("1.2.3");
  await expect(pluginRow(dialog, "Test plugin")).not.toContainText("when the engine restarts");
  await dialog.getByRole("button", { name: "Close" }).click();

  await graph.page.getByRole("button", { name: "Add module" }).click();
  await graph.menu.getByRole("menuitem", { name: "Test module" }).click();
  await expect.poll(async () => (await engine.modules()).map((module) => module.type)).toEqual(["Test"]);
  const [module] = await engine.modules();

  await graph.node(module.id).getByRole("button", { name: "Value 0" }).click();
  await expect(graph.node(module.id).getByRole("button", { name: "Value 1" })).toBeVisible();
  await expect.poll(async () => ((await engine.module(module.id))?.state as { value: number }).value).toBe(1);

  // removing it
  dialog = await openSettings(graph);
  await dialog.getByRole("button", { name: "Remove Test plugin" }).click();
  await expect(pluginRow(dialog, "Test plugin")).toContainText("Removed when the engine restarts");

  await app.restartEngine();

  await expect(dialog.getByText("Test plugin", { exact: true })).toHaveCount(0);
  await dialog.getByRole("button", { name: "Close" }).click();
  await graph.page.getByRole("button", { name: "Add module" }).click();
  await expect(graph.menu.getByRole("menuitem", { name: "Gain", exact: true })).toBeVisible();
  await expect(graph.menu.getByRole("menuitem", { name: "Test module" })).toHaveCount(0);
});

test("in the shell, the settings offer to restart the engine to apply plugin changes", async ({ graph, engine }) => {
  const shell = await FakeShell.install(graph.page, { ...developmentShell, canRestartEngine: true });
  await graph.open();
  const dialog = await openSettings(graph);

  await dialog.locator("input[type=file]").setInputFiles(packagePath());
  await expect(pluginRow(dialog, "Test plugin")).toContainText("Installed when the engine restarts");
  await dialog.getByRole("button", { name: "Restart engine to apply" }).click();

  expect(await shell.messages()).toEqual([{ type: "restartEngine" }]);
  expect((await engine.plugins()).map((plugin) => plugin.id)).toContain("Test");
});

test("a file that isn't a plugin package is rejected", async ({ graph, engine }) => {
  await graph.open();
  const dialog = await openSettings(graph);

  await dialog.locator("input[type=file]").setInputFiles({
    name: "plugin.zip",
    mimeType: "application/zip",
    buffer: Buffer.from("not a zip"),
  });

  await expect(graph.notification("Installing the plugin failed")).toBeVisible();
  expect((await engine.plugins()).map((plugin) => plugin.id)).toEqual(["Main"]);
});
