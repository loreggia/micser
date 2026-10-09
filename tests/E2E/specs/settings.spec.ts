import { expect, test, type Graph } from "../fixtures";

async function openSettings(graph: Graph) {
  await graph.open();
  await graph.page.getByRole("button", { name: "Settings" }).click();
  return graph.page.getByRole("dialog", { name: "Settings" });
}

test("display preferences apply right away and are kept by the engine", async ({ graph, engine }) => {
  const dialog = await openSettings(graph);

  await dialog.getByRole("switch", { name: "Snap modules to the grid" }).click();
  await dialog.getByRole("switch", { name: /Show stream statistics/ }).click();

  await expect
    .poll(() => engine.preferences())
    .toEqual({ showStreamStatistics: true, snapToGrid: false, language: null, showChannelsByDefault: false });

  await graph.page.reload();
  await graph.page.getByRole("button", { name: "Settings" }).click();
  await expect(dialog.getByRole("switch", { name: "Snap modules to the grid" })).not.toBeChecked();
});

test("without snapping, a moved module keeps its exact position", async ({ graph, engine }) => {
  const module = await engine.addModule("Gain", { x: 0, y: 0 });
  const dialog = await openSettings(graph);
  await dialog.getByRole("switch", { name: "Snap modules to the grid" }).click();
  await expect.poll(async () => (await engine.preferences()).snapToGrid).toBe(false);
  await dialog.getByRole("button", { name: "Close" }).click();
  await expect(dialog).toBeHidden();

  await graph.moveBy(graph.node(module.id), 213, 127);

  await expect.poll(async () => (await engine.module(module.id))?.position?.x).not.toBe(0);
  const { x, y } = (await engine.module(module.id))!.position!;
  expect([x % 20, y % 20]).not.toEqual([0, 0]);
});

test("new modules show their channels if the preference is on", async ({ graph, engine }) => {
  const dialog = await openSettings(graph);
  await dialog.getByRole("switch", { name: "Show the channels of new modules" }).click();
  await expect.poll(async () => (await engine.preferences()).showChannelsByDefault).toBe(true);
  await dialog.getByRole("button", { name: "Close" }).click();
  await expect(dialog).toBeHidden();

  await graph.page.getByRole("button", { name: "Add module" }).click();
  await graph.menu.getByRole("menuitem", { name: "Gain", exact: true }).click();

  await expect.poll(async () => (await engine.modules()).map((module) => module.showChannels)).toEqual([true]);
  const [module] = await engine.modules();
  await expect(graph.handle(graph.node(module.id), "Input:1")).toBeVisible();
});

test("lists the loaded plugins", async ({ graph }) => {
  const dialog = await openSettings(graph);

  await expect(dialog.getByText("Plugins", { exact: true })).toBeVisible();
  await expect(dialog.getByText("Main", { exact: true })).toBeVisible();
  await expect(dialog.getByText("built in")).toBeVisible();
  // a built-in plugin can't be removed
  await expect(dialog.getByRole("button", { name: "Remove Main" })).toHaveCount(0);
});
