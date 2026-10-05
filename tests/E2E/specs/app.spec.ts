import { expect, test } from "../fixtures";

test("shows the engine's status and module types", async ({ graph, page }) => {
  await graph.open();

  await expect(page.getByText(/kHz · [\d.]+ ms blocks/)).toBeVisible();
  await page.getByRole("button", { name: "Add module" }).click();
  for (const title of ["Gain", "Equalizer", "Input device", "Output device"]) {
    await expect(graph.menu.getByRole("menuitem", { name: title, exact: true })).toBeVisible();
  }
});

test("adds a module from the toolbar and selects it", async ({ graph, engine, page }) => {
  await graph.open();

  await page.getByRole("button", { name: "Add module" }).click();
  await graph.menu.getByRole("menuitem", { name: "Gain", exact: true }).click();

  await expect.poll(async () => (await engine.modules()).map((module) => module.type)).toEqual(["Gain"]);
  const [module] = await engine.modules();
  await expect(graph.node(module.id)).toBeVisible();
  await expect(graph.node(module.id)).toHaveClass(/selected/);

  await page.reload();
  await expect(graph.node(module.id)).toBeVisible();
});

test("follows changes made elsewhere", async ({ graph, engine }) => {
  await graph.open();

  const module = await engine.addModule("Gain", { x: 0, y: 0 }, { name: "From the API" });
  await expect(graph.node(module.id)).toContainText("From the API");

  await engine.updateModule({ ...module, name: "Renamed" });
  await expect(graph.node(module.id)).toContainText("Renamed");

  await engine.deleteModule(module.id);
  await expect(graph.node(module.id)).toHaveCount(0);
});

test("keeps two windows in sync", async ({ graph, engine, browser }) => {
  const module = await engine.addModule("Gain", { x: 0, y: 0 });
  await graph.open();
  const other = await browser.newPage();
  await other.goto(graph.page.url());

  await graph.node(module.id).getByRole("button", { name: "Mute" }).click();

  await expect(
    other.locator(`.react-flow__node[data-id="${module.id}"]`).getByRole("button", { name: "Unmute" })
  ).toBeVisible();
  await other.close();
});
