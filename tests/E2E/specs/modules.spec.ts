import type { ModuleDto } from "@micser/web-sdk";
import { expect, test, type EngineApi } from "../fixtures";

async function state<T extends ModuleDto["type"]>(engine: EngineApi, module: ModuleDto & { type: T }) {
  return (await engine.module(module.id)) as (ModuleDto & { type: T }) | undefined;
}

test("a widget's change reaches the engine", async ({ graph, engine }) => {
  const module = (await engine.addModule("Gain", { x: 0, y: 0 })) as ModuleDto & { type: "Gain" };
  await graph.open();

  await graph.node(module.id).getByRole("slider", { name: "Gain" }).focus();
  await graph.page.keyboard.press("ArrowRight");
  await expect(graph.node(module.id)).toContainText("+0.5 dB");
  await graph.page.keyboard.press("ArrowRight");

  await expect(graph.node(module.id)).toContainText("+1.0 dB");
  await expect.poll(async () => (await state(engine, module))?.state.gain).toBe(1);
});

test("the node's controls change the module", async ({ graph, engine }) => {
  const module = await engine.addModule("Gain", { x: 0, y: 0 });
  await graph.open();
  const node = graph.node(module.id);

  await node.getByRole("button", { name: "Mute" }).click();
  await expect.poll(async () => (await engine.module(module.id))?.isMuted).toBe(true);

  await node.getByRole("slider", { name: "Volume" }).focus();
  await graph.page.keyboard.press("Home");
  await expect(node).toContainText("0%");
  await expect.poll(async () => (await engine.module(module.id))?.volume).toBe(0);

  await node.getByRole("button", { name: "Collapse" }).click();
  await expect(node.getByRole("slider")).toHaveCount(0);
  await expect.poll(async () => (await engine.module(module.id))?.isCollapsed).toBe(true);
  // the ports stay
  await expect(graph.port(node, "input")).toBeVisible();
  await expect(graph.port(node, "output")).toBeVisible();
});

test("a module is renamed by double-clicking its title", async ({ graph, engine }) => {
  const module = await engine.addModule("Gain", { x: 0, y: 0 });
  await graph.open();
  const node = graph.node(module.id);

  await graph.title(node).getByText("Gain", { exact: true }).dblclick();
  await node.getByRole("textbox", { name: "Module name" }).fill("Voice");
  await graph.page.keyboard.press("Enter");

  await expect.poll(async () => (await engine.module(module.id))?.name).toBe("Voice");
  await expect(graph.title(node)).toHaveText("Voice");
  // the type's title below the name
  await expect(node.locator(".fui-CardHeader__description")).toHaveText("Gain");
});

test("a moved module keeps its position on the grid", async ({ graph, engine }) => {
  const module = await engine.addModule("Gain", { x: 0, y: 0 });
  await graph.open();

  await graph.moveBy(graph.node(module.id), 213, 127);

  await expect.poll(async () => (await engine.module(module.id))?.position?.x).not.toBe(0);
  const { x, y } = (await engine.module(module.id))!.position!;
  expect([x % 20, y % 20]).toEqual([0, 0]);
});

test("a module is resized with its handle, its widget scrolls, and its size is reset", async ({ graph, engine }) => {
  const module = await engine.addModule("Compressor", { x: 0, y: 0 });
  await graph.open();
  const node = graph.node(module.id);
  const naturalHeight = await node.evaluate((element: HTMLElement) => element.offsetHeight);

  const handle = (await node.locator(".react-flow__resize-control").boundingBox())!;
  const nodeBox = (await node.boundingBox())!;
  const start = { x: handle.x + handle.width / 2, y: handle.y + handle.height / 2 };
  await graph.page.mouse.move(start.x, start.y);
  await graph.page.mouse.down();
  await graph.page.mouse.move(start.x + nodeBox.width / 4, start.y - nodeBox.height / 3, { steps: 10 });
  await graph.page.mouse.up();

  await expect.poll(async () => (await engine.module(module.id))?.size).toBeTruthy();
  const size = (await engine.module(module.id))!.size!;
  expect(size.height).toBeLessThan(naturalHeight);
  await expect.poll(() => node.evaluate((element: HTMLElement) => element.offsetHeight)).toBe(size.height);
  // the widget scrolls
  const scrolls = await node.evaluate((element) =>
    [...element.querySelectorAll("div")].some(
      (div) => getComputedStyle(div).overflowY === "auto" && div.scrollHeight > div.clientHeight
    )
  );
  expect(scrolls).toBe(true);

  await node.getByRole("button", { name: "More" }).click();
  await graph.menu.getByRole("menuitem", { name: "Reset size" }).click();

  await expect.poll(async () => (await engine.module(module.id))?.size).toBeNull();
  await expect.poll(() => node.evaluate((element: HTMLElement) => element.offsetHeight)).toBe(naturalHeight);
});

test("a resized module fits its content's height while collapsed", async ({ graph, engine }) => {
  const sized = await engine.addModule("Compressor", { x: 0, y: 0 });
  const unsized = await engine.addModule("Compressor", { x: 400, y: 0 });
  await engine.updateModule({ ...sized, size: { width: 300, height: 400 } });
  await engine.updateModule({ ...unsized, isCollapsed: true });
  await graph.open();
  const node = graph.node(sized.id);
  const height = (id: string) => graph.node(id).evaluate((element: HTMLElement) => element.offsetHeight);
  await expect.poll(() => height(sized.id)).toBe(400);

  await node.getByRole("button", { name: "Collapse" }).click();
  await expect.poll(() => height(sized.id)).toBe(await height(unsized.id));
  expect(await node.evaluate((element: HTMLElement) => element.offsetWidth)).toBe(300);

  await node.getByRole("button", { name: "Expand" }).click();
  await expect.poll(() => height(sized.id)).toBe(400);
});

test("a module is deleted from its menu", async ({ graph, engine }) => {
  const module = await engine.addModule("Gain", { x: 0, y: 0 });
  await graph.open();

  await graph.node(module.id).getByRole("button", { name: "More" }).click();
  await graph.menu.getByRole("menuitem", { name: "Delete" }).click();

  await expect(graph.node(module.id)).toHaveCount(0);
  await expect.poll(() => engine.modules()).toEqual([]);
});

test("the Delete key deletes the selected modules", async ({ graph, engine }) => {
  const first = await engine.addModule("Gain", { x: 0, y: 0 });
  const second = await engine.addModule("Gain", { x: 400, y: 0 });
  await graph.open();

  await graph.node(first.id).click({ position: { x: 8, y: 24 } });
  await graph.page.keyboard.press("Delete");

  await expect(graph.node(first.id)).toHaveCount(0);
  await expect.poll(async () => (await engine.modules()).map((module) => module.id)).toEqual([second.id]);
});

test("the Delete key in a module's name field doesn't delete the module", async ({ graph, engine }) => {
  const module = await engine.addModule("Gain", { x: 0, y: 0 }, { name: "Voice" });
  await graph.open();
  const node = graph.node(module.id);

  await node.click({ position: { x: 8, y: 24 } });
  await node.getByText("Voice", { exact: true }).dblclick();
  await graph.page.keyboard.press("End");
  await graph.page.keyboard.press("Delete");
  await graph.page.keyboard.press("Escape");

  await expect(node).toBeVisible();
  expect(await engine.module(module.id)).toBeDefined();
});
