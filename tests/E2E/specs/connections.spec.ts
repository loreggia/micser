import { expect, test } from "../fixtures";

test("dragging from an output to an input connects the modules", async ({ graph, engine }) => {
  const source = await engine.addModule("Gain", { x: 0, y: 0 });
  const target = await engine.addModule("Gain", { x: 400, y: 0 });
  await graph.open();

  await graph.connect(graph.port(graph.node(source.id), "output"), graph.port(graph.node(target.id), "input"));

  await expect
    .poll(async () => (await engine.connections()).map((c) => [c.sourceModuleId, c.targetModuleId]))
    .toEqual([[source.id, target.id]]);
  await expect(graph.edges).toHaveCount(1);
});

test("a rejected connection shows why", async ({ graph, engine }) => {
  const first = await engine.addModule("Gain", { x: 0, y: 0 });
  const second = await engine.addModule("Gain", { x: 400, y: 0 });
  await engine.connect(first, second);
  await graph.open();

  // second → first would be a cycle
  await graph.connect(graph.port(graph.node(second.id), "output"), graph.port(graph.node(first.id), "input"));

  await expect(graph.notification("Connecting failed")).toBeVisible();
  expect(await engine.connections()).toHaveLength(1);
  await expect(graph.edges).toHaveCount(1);
});

test("a selected connection is deleted with the Delete key", async ({ graph, engine }) => {
  const first = await engine.addModule("Gain", { x: 0, y: 0 });
  const second = await engine.addModule("Gain", { x: 400, y: 0 });
  await engine.connect(first, second);
  await graph.open();

  await graph.clickConnection(graph.node(first.id), graph.node(second.id));
  await expect(graph.edges.first()).toHaveClass(/selected/);
  await graph.page.keyboard.press("Delete");

  await expect(graph.edges).toHaveCount(0);
  await expect.poll(() => engine.connections()).toEqual([]);
  // the modules stay
  expect(await engine.modules()).toHaveLength(2);
});

test("dropping a connection on empty space adds a connected module", async ({ graph, engine }) => {
  const source = await engine.addModule("Gain", { x: 0, y: 0 });
  await graph.open();
  const output = graph.port(graph.node(source.id), "output");

  const box = (await output.boundingBox())!;
  await output.hover();
  await graph.page.mouse.down();
  await graph.page.mouse.move(box.x + 300, box.y + 50, { steps: 5 });
  await graph.page.mouse.up();

  // only module types with an input
  await expect(graph.menu.getByRole("menuitem", { name: "Gain", exact: true })).toBeVisible();
  await expect(graph.menu.getByRole("menuitem", { name: "Input device" })).toHaveCount(0);
  await graph.menu.getByRole("menuitem", { name: "Gain", exact: true }).click();

  await expect.poll(async () => (await engine.modules()).length).toBe(2);
  const added = (await engine.modules()).find((module) => module.id !== source.id)!;
  await expect
    .poll(async () => (await engine.connections()).map((c) => [c.sourceModuleId, c.targetModuleId]))
    .toEqual([[source.id, added.id]]);
});
