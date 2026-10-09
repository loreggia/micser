import { Toaster } from "@fluentui/react-components";
import {
  getGetModuleTypesQueryKey,
  getGetPreferencesQueryKey,
  type ModuleDto,
  type ModuleTypeDto,
  type UiPreferencesDto,
  type WidgetDefinition,
} from "@micser/web-sdk";
import { createTestQueryClient, testModule, TestProviders } from "@micser/web-sdk/testing";
import type { QueryClient } from "@tanstack/react-query";
import { ReactFlow, ReactFlowProvider, useReactFlow, type Edge, type Node } from "@xyflow/react";
import "@xyflow/react/dist/style.css";
import type { ReactNode } from "react";
import { afterEach, beforeEach, describe, expect, test, vi } from "vitest";
import { page } from "vitest/browser";
import { renderHook } from "vitest-browser-react";
import { toasterId } from "../../notifications";
import { PluginWidgetsContext } from "../../plugins";
import type { ModuleNodeType } from "../ModuleNode";
import { pendingPlacement, pendingSelection } from "../newModules";
import { useAddModule, useModuleTypeChoices } from "../useAddModule";

/** Modules render 100 × 50, so their measured size is known. */
const nodeTypes = { module: () => <div style={{ width: 100, height: 50 }} /> };

function moduleNode(id: string, x: number, y: number, properties: Partial<ModuleNodeType> = {}): ModuleNodeType {
  return {
    id,
    type: "module",
    position: { x, y },
    data: { module: testModule("Gain", { gain: 0 }, { id }) },
    ...properties,
  };
}

let queryClient: QueryClient;
let widgets: Map<string, WidgetDefinition>;
let nodes: Node[];
let edges: Edge[];
let created: { type: string; position: { x: number; y: number }; subgraphId?: string; showChannels: boolean }[];
let createResponse: (request: (typeof created)[number]) => Response;

function wrapper({ children }: { children: ReactNode }) {
  return (
    <TestProviders queryClient={queryClient}>
      <PluginWidgetsContext.Provider value={{ widgets, isLoading: false }}>
        <ReactFlowProvider>
          {children}
          <div style={{ width: 800, height: 600 }}>
            <ReactFlow defaultNodes={nodes} defaultEdges={edges} nodeTypes={nodeTypes} onError={() => {}} />
          </div>
        </ReactFlowProvider>
      </PluginWidgetsContext.Provider>
      <Toaster toasterId={toasterId} />
    </TestProviders>
  );
}

async function renderAddModule() {
  const hook = await renderHook(() => ({ add: useAddModule(), flow: useReactFlow() }), { wrapper });
  // positions depend on the measured sizes
  await expect.poll(() => hook.result.current.flow.getNodes().every((node) => node.measured?.width === 100)).toBe(true);
  return hook;
}

beforeEach(() => {
  queryClient = createTestQueryClient();
  widgets = new Map();
  nodes = [];
  edges = [];
  created = [];
  createResponse = (request) =>
    Response.json({ ...testModule("Gain", { gain: 0 }, { id: "new-1" }), ...request } as ModuleDto);

  const fetch = window.fetch.bind(window);
  vi.spyOn(window, "fetch").mockImplementation(async (input, init) => {
    if (String(input) !== "/api/modules" || init?.method !== "POST") {
      return fetch(input, init);
    }

    const request = JSON.parse(String(init.body)) as (typeof created)[number];
    created.push(request);
    return createResponse(request);
  });
});

afterEach(() => {
  vi.restoreAllMocks();
  pendingPlacement.clear();
  pendingSelection.clear();
});

describe("useAddModule", () => {
  test("places a module of a type on the graph with that type's size", async () => {
    nodes = [moduleNode("gain-1", 0, 0)];
    const { result } = await renderAddModule();

    await result.current.add("Gain", { x: 0, y: -80 });

    expect(created).toEqual([{ type: "Gain", position: { x: 0, y: -80 }, showChannels: false }]);
  });

  test("places a module of a new type with the default size", async () => {
    nodes = [moduleNode("gain-1", 0, 0)];
    const { result } = await renderAddModule();

    await result.current.add("Compressor", { x: 0, y: -80 });

    // 140 px high, plus a grid step above the module
    expect(created).toEqual([{ type: "Compressor", position: { x: 0, y: -160 }, showChannels: false }]);
  });

  test("places a module in a subgraph below its header, ignoring the modules outside", async () => {
    nodes = [moduleNode("gain-1", 40, 60)];
    const { result } = await renderAddModule();

    await result.current.add("Gain", { x: 0, y: 0 }, "subgraph-1");

    expect(created).toEqual([
      { type: "Gain", position: { x: 40, y: 60 }, subgraphId: "subgraph-1", showChannels: false },
    ]);
  });

  test("shows the channels of the module if the preference says so", async () => {
    queryClient.setQueryData(getGetPreferencesQueryKey(), {
      showStreamStatistics: false,
      snapToGrid: true,
      showChannelsByDefault: true,
    } satisfies UiPreferencesDto);
    const { result } = await renderAddModule();

    await result.current.add("Gain", { x: 0, y: 0 });

    expect(created.map((request) => request.showChannels)).toEqual([true]);
  });

  test("adds near the center of the view without a position", async () => {
    const { result } = await renderAddModule();

    const module = await result.current.add("Gain");

    expect(pendingPlacement.get(module!.id)).toEqual({ x: 280, y: 200 });
  });

  test("selects the new module once it appears", async () => {
    const { result } = await renderAddModule();

    const module = await result.current.add("Gain", { x: 0, y: 0 });

    expect(module?.id).toBe("new-1");
    expect(pendingSelection.has("new-1")).toBe(true);
    expect(pendingPlacement.get("new-1")).toEqual({ x: 0, y: 0 });
  });

  test("selects the new module only, if it's already on the graph", async () => {
    nodes = [moduleNode("gain-1", 0, 0, { selected: true }), moduleNode("new-1", 300, 0)];
    edges = [{ id: "connection-1", source: "gain-1", target: "new-1", selected: true }];
    const { result } = await renderAddModule();

    await result.current.add("Gain", { x: 0, y: 200 });

    await expect
      .poll(() => result.current.flow.getNodes().map((node) => [node.id, node.selected]))
      .toEqual([
        ["gain-1", false],
        ["new-1", true],
      ]);
    expect(result.current.flow.getEdges()[0].selected).toBe(false);
    expect(pendingSelection.size).toBe(0);
  });

  test("shows why adding failed", async () => {
    createResponse = () => Response.json({ title: "Unknown module type Reverb." }, { status: 400 });
    const { result } = await renderAddModule();

    await expect(result.current.add("Reverb", { x: 0, y: 0 })).resolves.toBeUndefined();

    await expect.element(page.getByText("Adding the module failed")).toBeVisible();
    await expect.element(page.getByText("Unknown module type Reverb.")).toBeVisible();
  });
});

describe("useModuleTypeChoices", () => {
  test("lists the module types by their widgets' titles", async () => {
    const moduleType = (type: string): ModuleTypeDto => ({
      type,
      inputs: [],
      outputs: [],
      defaultState: {},
      supportsBypass: false,
      supportsChannelCount: false,
      channelCountInputs: [],
    });
    queryClient.setQueryData(getGetModuleTypesQueryKey(), [
      moduleType("Gain"),
      moduleType("Reverb"),
      moduleType("DeviceInput"),
    ]);
    widgets = new Map([
      ["Gain", { moduleType: "Gain", title: "Gain", component: () => null }],
      ["DeviceInput", { moduleType: "DeviceInput", title: "Input device", component: () => null }],
    ]);

    const { result } = await renderHook(() => useModuleTypeChoices(), { wrapper });

    // without a widget, the type name
    expect(result.current.map((choice) => [choice.type, choice.title])).toEqual([
      ["Gain", "Gain"],
      ["DeviceInput", "Input device"],
      ["Reverb", "Reverb"],
    ]);
  });
});
