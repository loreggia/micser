import type { ModuleDto } from "@micser/web-sdk";
import { expect, test, type EngineApi, type Graph } from "../fixtures";

async function selectNodes(graph: Graph, ...modules: ModuleDto[]) {
  await graph.node(modules[0].id).click({ position: { x: 8, y: 24 } });
  for (const module of modules.slice(1)) {
    await graph.node(module.id).click({ position: { x: 8, y: 24 }, modifiers: ["Control"] });
  }
}

async function subgraphIds(engine: EngineApi, ...modules: ModuleDto[]) {
  const all = await engine.modules();
  return modules.map((module) => all.find((m) => m.id === module.id)?.subgraphId ?? null);
}

async function groupedSubgraph(engine: EngineApi, graph: Graph) {
  const first = await engine.addModule("Gain", { x: 0, y: 0 });
  const second = await engine.addModule("Gain", { x: 400, y: 0 });
  await engine.connect(first, second);
  await graph.open();
  await selectNodes(graph, first, second);
  await graph.page.keyboard.press("Control+g");
  await expect.poll(async () => (await engine.subgraphs()).length).toBe(1);
  const [subgraph] = await engine.subgraphs();
  return { first, second, subgraph, node: graph.node(subgraph.id) };
}

test("Ctrl+G groups the selected modules", async ({ graph, engine }) => {
  const { first, second, subgraph, node } = await groupedSubgraph(engine, graph);

  await expect.poll(() => subgraphIds(engine, first, second)).toEqual([subgraph.id, subgraph.id]);
  await expect(node).toContainText("Subgraph");
});

test("a module's menu groups it", async ({ graph, engine }) => {
  const module = await engine.addModule("Gain", { x: 0, y: 0 });
  await graph.open();

  await graph.node(module.id).click({ button: "right", position: { x: 8, y: 24 } });
  await graph.menu.getByRole("menuitem", { name: "Group" }).click();

  await expect.poll(async () => (await engine.subgraphs()).length).toBe(1);
  await expect.poll(async () => (await engine.module(module.id))?.subgraphId).toBe((await engine.subgraphs())[0].id);
});

test("modules in a subgraph can't be grouped again", async ({ graph, engine }) => {
  const { first, second } = await groupedSubgraph(engine, graph);

  await graph.node(first.id).click({ button: "right", position: { x: 8, y: 24 } });
  await expect(graph.menu.getByRole("menuitem", { name: "Remove from subgraph" })).toBeVisible();
  await expect(graph.menu.getByRole("menuitem", { name: "Group" })).toHaveCount(0);
  await graph.page.keyboard.press("Escape");

  await selectNodes(graph, first, second);
  await graph.page.keyboard.press("Control+g");
  await graph.page.waitForTimeout(500);
  expect(await engine.subgraphs()).toHaveLength(1);
});

test("removing a module from its subgraph places it below the frame", async ({ graph, engine }) => {
  const { first, subgraph } = await groupedSubgraph(engine, graph);

  await graph.node(first.id).click({ button: "right", position: { x: 8, y: 24 } });
  await graph.menu.getByRole("menuitem", { name: "Remove from subgraph" }).click();

  await expect.poll(async () => (await engine.module(first.id))?.subgraphId ?? null).toBeNull();
  const moved = (await engine.module(first.id))!;
  expect(moved.position!.y).toBeGreaterThan(subgraph.position.y + subgraph.size.height);
});

test("a collapsed subgraph hides its modules and shows the outside ports", async ({ graph, engine }) => {
  const { first, second, node } = await groupedSubgraph(engine, graph);
  const outside = await engine.addModule("Gain", { x: 1000, y: 400 });
  await engine.connect(second, outside);

  await node.getByRole("button", { name: "Collapse" }).click();

  await expect(graph.node(first.id)).toBeHidden();
  await expect(graph.node(second.id)).toBeHidden();
  // the inside connection is hidden, the one to the outside goes to the subgraph's port for it
  await expect(graph.edges).toHaveCount(1);
  await expect(graph.port(node, "output")).toBeVisible();
  await expect(node.locator(".react-flow__handle.target")).toHaveCount(0);

  await node.getByRole("button", { name: "Expand" }).click();
  await expect(graph.node(first.id)).toBeVisible();
  await expect(graph.edges).toHaveCount(2);
});

test("a subgraph is renamed by double-clicking its title", async ({ graph, engine }) => {
  const { subgraph, node } = await groupedSubgraph(engine, graph);

  await node.getByText("Subgraph", { exact: true }).dblclick();
  await node.getByRole("textbox", { name: "Subgraph name" }).fill("Voice chain");
  await graph.page.keyboard.press("Enter");

  await expect.poll(async () => (await engine.subgraphs()).find((s) => s.id === subgraph.id)?.name).toBe("Voice chain");
});

test("mute and bypass of a subgraph apply to its modules", async ({ graph, engine }) => {
  const { first, node } = await groupedSubgraph(engine, graph);

  await node.getByRole("button", { name: "Mute all" }).click();

  await expect(graph.node(first.id).getByRole("button", { name: "Muted by subgraph" })).toBeDisabled();
  await expect.poll(async () => (await engine.subgraphs())[0].isMuted).toBe(true);
});

test("ungrouping keeps the modules", async ({ graph, engine }) => {
  const { first, second, node } = await groupedSubgraph(engine, graph);

  await node.getByRole("button", { name: "More" }).click();
  await graph.menu.getByRole("menuitem", { name: "Ungroup" }).click();

  await expect.poll(() => engine.subgraphs()).toEqual([]);
  expect(await subgraphIds(engine, first, second)).toEqual([null, null]);
});

test("deleting a subgraph deletes its modules", async ({ graph, engine }) => {
  const { node } = await groupedSubgraph(engine, graph);
  const outside = await engine.addModule("Gain", { x: 1000, y: 400 });

  await node.getByRole("button", { name: "More" }).click();
  await graph.menu.getByRole("menuitem", { name: "Delete" }).click();

  await expect.poll(() => engine.subgraphs()).toEqual([]);
  await expect.poll(async () => (await engine.modules()).map((module) => module.id)).toEqual([outside.id]);
});

test("the Delete key deletes a selected subgraph with its modules", async ({ graph, engine }) => {
  const { node } = await groupedSubgraph(engine, graph);

  await graph.pane.click({ position: { x: 5, y: 5 } });
  await node.getByText("Subgraph", { exact: true }).click();
  await graph.page.keyboard.press("Delete");

  await expect.poll(() => engine.subgraphs()).toEqual([]);
  await expect.poll(() => engine.modules()).toEqual([]);
});
