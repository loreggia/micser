import { expect, test, type EngineApi, type Graph } from "../fixtures";

async function subgraphWithModules(engine: EngineApi, graph: Graph) {
  const first = await engine.addModule("Gain", { x: 0, y: 0 });
  const second = await engine.addModule("Equalizer", { x: 400, y: 0 });
  await engine.connect(first, second);
  const subgraph = await engine.addSubgraph({
    name: "Voice chain",
    position: { x: -40, y: -60 },
    size: { width: 1000, height: 600 },
    moduleIds: [first.id, second.id],
  });
  await graph.open();
  return { subgraph, node: graph.node(subgraph.id) };
}

async function saveAsTemplate(graph: Graph, node: ReturnType<Graph["node"]>, name?: string) {
  await node.getByRole("button", { name: "More" }).click();
  await graph.menu.getByRole("menuitem", { name: "Save as template…" }).click();
  const dialog = graph.page.getByRole("dialog", { name: "Save as template" });
  if (name) {
    await dialog.getByRole("textbox", { name: "Template name" }).fill(name);
  }

  return dialog;
}

test("saves a subgraph as a template", async ({ graph, engine }) => {
  const { node } = await subgraphWithModules(engine, graph);

  const dialog = await saveAsTemplate(graph, node);
  // the subgraph's name is suggested
  await expect(dialog.getByRole("textbox", { name: "Template name" })).toHaveValue("Voice chain");
  await dialog.getByRole("button", { name: "Save" }).click();

  await expect(dialog).toBeHidden();
  await expect
    .poll(async () => (await engine.templates()).map((t) => [t.name, t.modules.length]))
    .toEqual([["Voice chain", 2]]);
  const [template] = await engine.templates();
  await expect.poll(async () => (await engine.subgraphs())[0].templateId).toBe(template.id);
  // the header shows the template's name next to the subgraph's
  await expect(node.getByText("Voice chain", { exact: true })).toHaveCount(2);
});

test("saving under a template's name replaces it", async ({ graph, engine }) => {
  const { node } = await subgraphWithModules(engine, graph);
  await (await saveAsTemplate(graph, node, "Chain")).getByRole("button", { name: "Save" }).click();
  await expect.poll(async () => (await engine.templates()).length).toBe(1);

  const dialog = await saveAsTemplate(graph, node);
  await expect(dialog.getByRole("textbox", { name: "Template name" })).toHaveValue("Chain");
  await dialog.getByRole("button", { name: "Replace" }).click();

  await expect.poll(async () => (await engine.templates()).map((t) => t.revision)).toEqual([2]);
});

test("adds a template from the toolbar", async ({ graph, engine }) => {
  const { subgraph, node } = await subgraphWithModules(engine, graph);
  await (await saveAsTemplate(graph, node, "Chain")).getByRole("button", { name: "Save" }).click();
  await expect.poll(async () => (await engine.templates()).length).toBe(1);
  const [template] = await engine.templates();

  await graph.page.getByRole("button", { name: "Add module" }).click();
  await graph.menu.getByRole("menuitem", { name: "Templates" }).click();
  await graph.menu.getByRole("menuitem", { name: "Chain" }).click();

  await expect.poll(async () => (await engine.subgraphs()).length).toBe(2);
  const added = (await engine.subgraphs()).find((s) => s.id !== subgraph.id)!;
  expect(added.templateId).toBe(template.id);
  await expect(graph.node(added.id)).toBeVisible();
  await expect.poll(async () => (await engine.modules()).length).toBe(4);
  await expect.poll(async () => (await engine.connections()).length).toBe(2);
});

test("the templates dialog lists, renames and removes templates", async ({ graph, engine }) => {
  const { node } = await subgraphWithModules(engine, graph);
  await (await saveAsTemplate(graph, node, "Chain")).getByRole("button", { name: "Save" }).click();
  await expect.poll(async () => (await engine.templates()).length).toBe(1);

  await graph.page.getByRole("button", { name: "Add module" }).click();
  await graph.menu.getByRole("menuitem", { name: "Templates" }).click();
  await graph.menu.getByRole("menuitem", { name: "Manage templates…" }).click();
  const dialog = graph.page.getByRole("dialog", { name: "Subgraph templates" });

  await expect(dialog).toContainText("2 modules · used by 1 subgraph");

  await dialog.getByText("Chain", { exact: true }).dblclick();
  await dialog.getByRole("textbox", { name: "Template name" }).fill("Voice");
  await graph.page.keyboard.press("Enter");
  await expect.poll(async () => (await engine.templates()).map((t) => t.name)).toEqual(["Voice"]);

  await dialog.getByRole("button", { name: "Remove (subgraphs created from it stay)" }).click();
  await dialog.getByRole("button", { name: "Remove", exact: true }).click();
  await expect.poll(() => engine.templates()).toEqual([]);
  // the subgraph stays, without its template
  await expect.poll(async () => (await engine.subgraphs()).map((s) => s.templateId ?? null)).toEqual([null]);
});

test("a subgraph from a built-in template is saved as a custom template", async ({ graph, engine }) => {
  await graph.open();
  await graph.page.getByRole("button", { name: "Add module" }).click();
  await graph.menu.getByRole("menuitem", { name: "Templates" }).click();
  await graph.menu.getByRole("menuitem", { name: "Night mode" }).click();
  await expect.poll(async () => (await engine.subgraphs()).length).toBe(1);
  const [subgraph] = await engine.subgraphs();
  const node = graph.node(subgraph.id);

  const dialog = await saveAsTemplate(graph, node);
  const name = dialog.getByRole("textbox", { name: "Template name" });
  await expect(name).toHaveValue("Night mode (custom)");
  // a built-in template can't be replaced
  await name.fill("night mode");
  await expect(dialog).toContainText("A built-in template has this name.");
  await expect(dialog.getByRole("button", { name: "Save" })).toBeDisabled();

  await name.fill("Night mode (custom)");
  await dialog.getByRole("button", { name: "Save" }).click();

  await expect.poll(async () => (await engine.templates()).map((t) => t.name)).toEqual(["Night mode (custom)"]);
  const [template] = await engine.templates();
  await expect.poll(async () => (await engine.subgraphs())[0].templateId).toBe(template.id);
});

test("the templates dialog shows built-in templates without rename and remove", async ({ graph, engine }) => {
  const builtIn = await engine.builtInTemplates();
  await graph.open();
  await graph.page.getByRole("button", { name: "Add module" }).click();
  await graph.menu.getByRole("menuitem", { name: "Templates" }).click();
  await graph.menu.getByRole("menuitem", { name: "Manage templates…" }).click();
  const dialog = graph.page.getByRole("dialog", { name: "Subgraph templates" });

  await expect(dialog.getByText("Built-in", { exact: true })).toHaveCount(builtIn.length);
  await expect(dialog.getByRole("button", { name: "Remove (subgraphs created from it stay)" })).toHaveCount(0);
  await dialog.getByText("Night mode", { exact: true }).dblclick();
  await expect(dialog.getByRole("textbox", { name: "Template name" })).toHaveCount(0);
});
