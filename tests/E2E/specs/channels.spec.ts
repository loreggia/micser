import type { ModuleDto } from "@micser/web-sdk";
import { expect, test } from "../fixtures";

test("single channels are shown, connected and hidden again", async ({ graph, engine }) => {
  const source = await engine.addModule("Gain", { x: 0, y: 0 });
  const target = await engine.addModule("Gain", { x: 500, y: 0 });
  await engine.updateModule({ ...source, channelCount: 4 } as ModuleDto);
  await graph.open();
  const sourceNode = graph.node(source.id);
  const targetNode = graph.node(target.id);

  for (const node of [sourceNode, targetNode]) {
    await graph.openMoreMenu(node);
    await graph.menu.getByRole("menuitemcheckbox", { name: "Show channels" }).click();
    await graph.page.keyboard.press("Escape");
  }

  await expect.poll(async () => (await engine.modules()).every((m) => m.showChannels)).toBe(true);
  // 4 channels with their speakers on the source, stereo on the target's input on Auto
  await expect(sourceNode.getByText("4 (BR)")).toHaveCount(2);
  await expect(graph.handle(targetNode, "Input:1")).toBeVisible();

  await graph.connect(graph.handle(sourceNode, "Output:2"), graph.handle(targetNode, "Input:1"));

  await expect
    .poll(async () => (await engine.connections()).map((c) => [c.sourceChannel, c.targetChannel]))
    .toEqual([[2, 1]]);
  await expect(graph.edges).toHaveCount(1);

  await graph.openMoreMenu(targetNode);
  await expect(graph.menu.getByRole("menuitemcheckbox", { name: "Show channels" })).toHaveAttribute(
    "aria-disabled",
    "true"
  );
  await graph.page.keyboard.press("Escape");

  await engine.disconnect((await engine.connections())[0].id);
  await expect(graph.edges).toHaveCount(0);
  await graph.openMoreMenu(targetNode);
  await graph.menu.getByRole("menuitemcheckbox", { name: "Show channels" }).click();

  await expect.poll(async () => (await engine.module(target.id))?.showChannels).toBe(false);
  await expect(graph.handle(targetNode, "Input:0")).toHaveCount(0);
});

test("the button between the ports shows and hides the channels", async ({ graph, engine }) => {
  const module = await engine.addModule("Gain", { x: 0, y: 0 });
  await graph.open();
  const node = graph.node(module.id);

  await node.getByRole("button", { name: "Show channels" }).click();

  await expect.poll(async () => (await engine.module(module.id))?.showChannels).toBe(true);
  await expect(graph.handle(node, "Input:1")).toBeVisible();

  await node.getByRole("button", { name: "Hide channels" }).click();

  await expect.poll(async () => (await engine.module(module.id))?.showChannels).toBe(false);
  await expect(graph.handle(node, "Input:0")).toHaveCount(0);
  await expect(graph.handle(node, "Input")).toBeVisible();
});

test("a channel connector that appears next to another one starts connections", async ({ graph, engine }) => {
  const module = await engine.addModule("Gain", { x: 0, y: 0 });
  await engine.updateModule({ ...module, showChannels: true });
  const first = await engine.addModule("Gain", { x: 500, y: -200 });
  const second = await engine.addModule("Gain", { x: 500, y: 200 });
  await graph.open();
  const node = graph.node(module.id);
  await expect(graph.handle(node, "Input:1")).toBeVisible();
  const height = (await node.boundingBox())!.height;

  // the output's channel 2 appears in the row of the input's channel 2, so the node keeps its size
  await engine.connect(module, first, { sourceChannel: 1 });
  await expect(graph.handle(node, "Output:1")).toBeVisible();
  expect((await node.boundingBox())!.height).toBe(height);

  await graph.connect(graph.handle(node, "Output:1"), graph.port(graph.node(second.id), "input"));

  await expect
    .poll(async () => (await engine.connections()).map((c) => [c.targetModuleId, c.sourceChannel]))
    .toContainEqual([second.id, 1]);
});

test("the channel count is chosen from the menu", async ({ graph, engine }) => {
  const module = await engine.addModule("Gain", { x: 0, y: 0 });
  await engine.updateModule({ ...module, showChannels: true });
  await graph.open();
  const node = graph.node(module.id);

  await graph.openMoreMenu(node);
  await graph.menu.getByRole("menuitem", { name: "Channels" }).click();
  await graph.menu.getByRole("menuitemradio", { name: "5.1" }).click();

  await expect.poll(async () => (await engine.module(module.id))?.channelCount).toBe(6);
  await expect(node.getByText("6 (SR)").first()).toBeVisible();

  await graph.openMoreMenu(node);
  await graph.menu.getByRole("menuitem", { name: "Channels" }).click();
  await graph.menu.getByRole("menuitem", { name: "Custom…" }).click();
  await graph.page.getByRole("spinbutton", { name: "Number of channels" }).fill("12");
  await graph.page.getByRole("button", { name: "Apply" }).click();

  await expect.poll(async () => (await engine.module(module.id))?.channelCount).toBe(12);
  await expect(graph.handle(node, "Output:11")).toBeVisible();
});

test("a connection to a channel the device doesn't have is dashed", async ({ graph, engine }) => {
  const gain = await engine.addModule("Gain", { x: 0, y: 0 });
  // without a device, the output has no channels
  const device = await engine.addModule("DeviceOutput", { x: 500, y: 0 });
  await engine.connect(gain, device, { targetChannel: 1 });
  await graph.open();

  await expect(graph.handle(graph.node(device.id), "Input:1")).toBeVisible();
  await expect(graph.edges.locator(".react-flow__edge-path")).toHaveCSS("stroke-dasharray", "6px, 4px");
});

test("a collapsed subgraph has a connector per channel that crosses its border", async ({ graph, engine }) => {
  const source = await engine.addModule("Gain", { x: 0, y: 0 });
  const mix = await engine.addModule("Gain", { x: 500, y: 0 }, { name: "Mix" });
  await engine.connect(source, mix, { targetChannel: 1 });
  const subgraph = await engine.addSubgraph({
    position: { x: 460, y: -60 },
    size: { width: 400, height: 400 },
    moduleIds: [mix.id],
  });
  await engine.updateSubgraph({ ...subgraph, isCollapsed: true });
  await graph.open();

  const node = graph.node(subgraph.id);
  await expect(node.getByText("Mix · 2 (R)")).toBeVisible();
  await expect(graph.handle(node, `in:${mix.id}:Input:1`)).toBeVisible();
  await expect(graph.edges).toHaveCount(1);
});
