import { expect, test } from "../fixtures";

test("right-clicking empty space adds a module there", async ({ graph, engine }) => {
  // the view fits these at the start, leaving its center empty; on an empty graph, the new module would move the view instead
  await engine.addModule("Gain", { x: 0, y: 0 });
  await engine.addModule("Gain", { x: 2000, y: 1200 });
  await graph.open();
  const pane = (await graph.pane.boundingBox())!;
  const click = { x: pane.width / 2, y: pane.height / 2 };

  await graph.pane.click({ button: "right", position: click });
  await expect(graph.menu.getByRole("menuitem", { name: "Templates" })).toBeVisible();
  await graph.menu.getByRole("menuitem", { name: "Gain", exact: true }).click();

  await expect.poll(async () => (await engine.modules()).length).toBe(3);
  const added = (await engine.modules()).at(-1)!;
  const box = (await graph.node(added.id).boundingBox())!;
  // at the click, give or take the grid
  expect(Math.abs(box.x - (pane.x + click.x))).toBeLessThan(30);
  expect(Math.abs(box.y - (pane.y + click.y))).toBeLessThan(30);
});

test("a module's menu deletes it", async ({ graph, engine }) => {
  const module = await engine.addModule("Gain", { x: 0, y: 0 });
  await graph.open();

  await graph.node(module.id).click({ button: "right", position: { x: 8, y: 24 } });
  await expect(graph.menu.getByRole("menuitem", { name: "Group" })).toBeVisible();
  await graph.menu.getByRole("menuitem", { name: "Delete" }).click();

  await expect.poll(() => engine.modules()).toEqual([]);
});

test("the browser's context menu stays available in text fields", async ({ graph, engine }) => {
  const module = await engine.addModule("Gain", { x: 0, y: 0 }, { name: "Voice" });
  await graph.open();
  const node = graph.node(module.id);

  await node.getByText("Voice", { exact: true }).dblclick();
  const prevented = await node
    .getByRole("textbox", { name: "Module name" })
    .evaluate((field) => !field.dispatchEvent(new MouseEvent("contextmenu", { bubbles: true, cancelable: true })));

  expect(prevented).toBe(false);
});
