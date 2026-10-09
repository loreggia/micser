import {
  EngineConnection,
  getGetConnectionsQueryKey,
  getGetPortLayoutsQueryKey,
  type ConnectionDto,
  type ModuleDto,
  type ModulePortLayoutsDto,
  type ModuleTypeDto,
} from "@micser/web-sdk";
import { createTestQueryClient, testModule, TestProviders } from "@micser/web-sdk/testing";
import type { QueryClient } from "@tanstack/react-query";
import { ReactFlow, ReactFlowProvider } from "@xyflow/react";
import "@xyflow/react/dist/style.css";
import { beforeEach, describe, expect, test, vi } from "vitest";
import { page, userEvent } from "vitest/browser";
import { render } from "vitest-browser-react";
import { ModuleActionsProvider } from "../ModuleActionsProvider";
import { ModuleNode, type ModuleNodeType } from "../ModuleNode";

const nodeTypes = { module: ModuleNode };
const gainType: ModuleTypeDto = {
  type: "Gain",
  inputs: ["Input"],
  outputs: ["Output"],
  defaultState: { gain: 0 },
  supportsBypass: true,
  supportsChannelCount: true,
};
const stereo: ModulePortLayoutsDto = {
  moduleId: "Gain-1",
  inputs: { Input: { channelCount: 2, speakers: ["FrontLeft", "FrontRight"] } },
  outputs: { Output: { channelCount: 2, speakers: ["FrontLeft", "FrontRight"] } },
};

let queryClient: QueryClient;
let connection: EngineConnection;

beforeEach(() => {
  queryClient = createTestQueryClient();
  queryClient.setQueryData(getGetPortLayoutsQueryKey(), [stereo]);
  queryClient.setQueryData(getGetConnectionsQueryKey(), []);
  connection = new EngineConnection(queryClient);
});

async function renderNode(module: ModuleDto, connections: ConnectionDto[] = []) {
  queryClient.setQueryData(getGetConnectionsQueryKey(), connections);
  // with a size, React Flow shows the node before measuring it, which doesn't always happen in these tests
  const node: ModuleNodeType = {
    id: module.id,
    type: "module",
    position: { x: 100, y: 100 },
    width: 300,
    height: 400,
    data: { module, moduleType: gainType },
  };
  return render(
    <TestProviders queryClient={queryClient} connection={connection}>
      <ModuleActionsProvider>
        <ReactFlowProvider>
          <div style={{ width: 800, height: 800 }}>
            <ReactFlow defaultNodes={[node]} nodeTypes={nodeTypes} onError={() => {}} />
          </div>
        </ReactFlowProvider>
      </ModuleActionsProvider>
    </TestProviders>
  );
}

function channelConnection(overrides: Partial<ConnectionDto>): ConnectionDto {
  return {
    id: "connection-1",
    sourceModuleId: "other",
    sourcePort: "Output",
    targetModuleId: "Gain-1",
    targetPort: "Input",
    targetChannel: 1,
    ...overrides,
  };
}

async function openMenu() {
  await page.getByRole("button", { name: "More" }).click();
}

describe("with its channels shown", () => {
  test("has a row with a connector per port and per channel, labelled with the speakers", async () => {
    const screen = await renderNode(testModule("Gain", { gain: 0 }, { showChannels: true }));

    await expect.element(screen.getByText("1 (L)").first()).toBeVisible();
    await expect.element(screen.getByText("2 (R)").first()).toBeVisible();
    await expect.element(screen.getByText("Input")).toBeVisible();
    for (const handle of ["Input", "Input:0", "Input:1", "Output", "Output:0", "Output:1"]) {
      expect(document.querySelector(`[data-handleid="${handle}"]`), handle).not.toBeNull();
    }
  });

  test("shows the channels that connections use beyond the layout", async () => {
    await renderNode(testModule("Gain", { gain: 0 }, { showChannels: true }), [
      channelConnection({ targetChannel: 4 }),
    ]);

    await expect.poll(() => document.querySelector('[data-handleid="Input:4"]')).not.toBeNull();
    expect(document.querySelector('[data-handleid="Output:4"]')).toBeNull();
  });
});

test("without its channels shown, has only the ports' connectors", async () => {
  const screen = await renderNode(testModule("Gain", { gain: 0 }));

  await expect.element(screen.getByRole("button", { name: "More" })).toBeVisible();
  expect(document.querySelector('[data-handleid="Input"]')).not.toBeNull();
  expect(document.querySelector('[data-handleid="Input:0"]')).toBeNull();
  expect(screen.getByText("1 (L)").query()).toBeNull();
});

describe("the menu", () => {
  test("turns showing the channels on", async () => {
    const update = vi.spyOn(connection, "updateModule").mockImplementation(() => {});
    const module = testModule("Gain", { gain: 0 });
    await renderNode(module);

    await openMenu();
    await page.getByRole("menuitemcheckbox", { name: "Show channels" }).click();
    // a checkbox item keeps the menu open
    await userEvent.keyboard("{Escape}");
    await expect.element(page.getByRole("menu")).not.toBeInTheDocument();

    expect(update).toHaveBeenCalledWith({ ...module, showChannels: true });
  });

  test("doesn't hide the channels while connections use them", async () => {
    await renderNode(testModule("Gain", { gain: 0 }, { showChannels: true }), [channelConnection({})]);

    await openMenu();

    await expect
      .element(page.getByRole("menuitemcheckbox", { name: "Show channels" }))
      .toHaveAttribute("aria-disabled", "true");
  });

  test("sets the channel count, offering only counts the connections fit in", async () => {
    const update = vi.spyOn(connection, "updateModule").mockImplementation(() => {});
    const module = testModule("Gain", { gain: 0 }, { showChannels: true });
    await renderNode(module, [channelConnection({ targetChannel: 1 })]);

    await openMenu();
    await page.getByRole("menuitem", { name: "Channels" }).click();
    await expect.element(page.getByRole("menuitemradio", { name: "Auto" })).toHaveAttribute("aria-checked", "true");
    await expect.element(page.getByRole("menuitemradio", { name: "Mono" })).toHaveAttribute("aria-disabled", "true");
    await page.getByRole("menuitemradio", { name: "5.1" }).click();

    expect(update).toHaveBeenCalledWith({ ...module, channelCount: 6 });
  });

  test("sets another channel count in a dialog", async () => {
    const update = vi.spyOn(connection, "updateModule").mockImplementation(() => {});
    const module = testModule("Gain", { gain: 0 });
    queryClient.setQueryData(["/api/modules"], [module]);
    await renderNode(module);

    await openMenu();
    await page.getByRole("menuitem", { name: "Channels" }).click();
    await page.getByRole("menuitem", { name: "Custom…" }).click();
    await page.getByRole("spinbutton", { name: "Number of channels" }).fill("12");
    await page.getByRole("button", { name: "Apply" }).click();

    expect(update).toHaveBeenCalledWith({ ...module, channelCount: 12 });
  });
});
