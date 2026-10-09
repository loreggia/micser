import { HubConnectionState } from "@microsoft/signalr";
import type { QueryClient } from "@tanstack/react-query";
import { afterEach, beforeEach, describe, expect, test, vi } from "vitest";
import { createTestQueryClient, testModule } from "../../../testing";
import {
  getGetConnectionsQueryKey,
  getGetDevicesQueryKey,
  getGetEngineStatusQueryKey,
  getGetModuleQueryKey,
  getGetModulesQueryKey,
  getGetPluginsQueryKey,
  getGetPortLayoutsQueryKey,
  getGetPreferencesQueryKey,
  getGetSubgraphsQueryKey,
  getGetSubgraphTemplatesQueryKey,
  updateModule,
  updateSubgraph,
  type ConnectionDto,
  type ModuleDto,
  type ModulePortLayoutsDto,
  type SubgraphDto,
} from "../../api";
import { EngineConnection, type EngineConnectionState } from "../EngineConnection";

/** The parts of a HubConnection that EngineConnection uses, recording the handlers it registers. */
interface FakeHub {
  handlers: Map<string, (...args: unknown[]) => unknown>;
  state: HubConnectionState;
  start: ReturnType<typeof vi.fn<() => Promise<void>>>;
  stop: ReturnType<typeof vi.fn<() => Promise<void>>>;
  invoke: ReturnType<typeof vi.fn<(method: string, ...args: unknown[]) => Promise<unknown>>>;
  reconnecting?: () => void;
  reconnected?: () => void;
  close?: () => void;
}

const hubs = vi.hoisted(() => [] as FakeHub[]);

vi.mock("@microsoft/signalr", async (importOriginal) => {
  const signalr = await importOriginal<typeof import("@microsoft/signalr")>();

  class HubConnectionBuilder {
    withUrl() {
      return this;
    }

    withAutomaticReconnect() {
      return this;
    }

    configureLogging() {
      return this;
    }

    build() {
      const hub: FakeHub = {
        handlers: new Map(),
        state: signalr.HubConnectionState.Disconnected,
        start: vi.fn(async () => {
          hub.state = signalr.HubConnectionState.Connected;
        }),
        stop: vi.fn(async () => {
          hub.state = signalr.HubConnectionState.Disconnected;
        }),
        invoke: vi.fn(() => Promise.resolve()),
      };
      hubs.push(hub);

      return Object.assign(hub, {
        on: (name: string, handler: (...args: unknown[]) => unknown) => hub.handlers.set(name, handler),
        onreconnecting: (callback: () => void) => (hub.reconnecting = callback),
        onreconnected: (callback: () => void) => (hub.reconnected = callback),
        onclose: (callback: () => void) => (hub.close = callback),
      });
    }
  }

  return { ...signalr, HubConnectionBuilder };
});

vi.mock("../../api", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../../api")>()),
  updateModule: vi.fn(),
  updateSubgraph: vi.fn(),
}));

const send = vi.mocked(updateModule);
const sendSubgraph = vi.mocked(updateSubgraph);
const original = testModule("Gain", { gain: 0 });
let queryClient: QueryClient;
let connection: EngineConnection;

function withGain(gain: number): ModuleDto {
  return { ...original, state: { gain } };
}

function cachedGain() {
  const modules = queryClient.getQueryData<ModuleDto[]>(getGetModulesQueryKey());
  return (modules?.[0] as typeof original | undefined)?.state.gain;
}

function hub() {
  return hubs.at(-1)!;
}

/** Calls the connection's handler of a hub event, as SignalR would. */
function receive(event: string, ...args: unknown[]) {
  const handler = hub().handlers.get(event);
  if (!handler) {
    throw new Error(`No handler for ${event}.`);
  }

  return handler(...args);
}

function recordStates() {
  const states: EngineConnectionState[] = [];
  connection.onStateChanged((state) => states.push(state));
  return states;
}

beforeEach(() => {
  vi.useFakeTimers();
  queryClient = createTestQueryClient();
  queryClient.setQueryData(getGetModulesQueryKey(), [original]);
  connection = new EngineConnection(queryClient);
  send.mockImplementation((_, module) => Promise.resolve(module));
  sendSubgraph.mockImplementation((_, subgraph) => Promise.resolve(subgraph));
});

afterEach(() => {
  vi.useRealTimers();
  send.mockReset();
  sendSubgraph.mockReset();
});

describe("module updates", () => {
  test("show in the cache right away", () => {
    connection.updateModule(withGain(3));

    expect(cachedGain()).toBe(3);
    expect(queryClient.getQueryData(getGetModuleQueryKey(original.id))).toEqual(withGain(3));
    expect(send).not.toHaveBeenCalled();
  });

  test("send the last of quick updates once, 80 ms after it", async () => {
    connection.updateModule(withGain(1));
    await vi.advanceTimersByTimeAsync(50);
    connection.updateModule(withGain(2));
    await vi.advanceTimersByTimeAsync(79);

    expect(send).not.toHaveBeenCalled();

    await vi.advanceTimersByTimeAsync(1);

    expect(send).toHaveBeenCalledOnce();
    expect(send).toHaveBeenCalledWith(original.id, withGain(2));
  });

  test("apply the engine's answer", async () => {
    send.mockResolvedValue(withGain(24));

    connection.updateModule(withGain(30));
    await vi.advanceTimersByTimeAsync(80);

    expect(cachedGain()).toBe(24);
  });

  test("keep a newer local value over the answer to an older one, and send it after", async () => {
    let answer!: (module: ModuleDto) => void;
    send.mockImplementationOnce(() => new Promise((resolve) => (answer = resolve)));

    connection.updateModule(withGain(1));
    await vi.advanceTimersByTimeAsync(80);
    connection.updateModule(withGain(2));
    await vi.advanceTimersByTimeAsync(80);

    expect(send).toHaveBeenCalledTimes(1);

    answer(withGain(1));
    await vi.advanceTimersByTimeAsync(0);

    expect(cachedGain()).toBe(2);
    expect(send).toHaveBeenCalledTimes(2);
    expect(send).toHaveBeenLastCalledWith(original.id, withGain(2));
  });

  test("report a failure and reload the modules", async () => {
    const errors: Error[] = [];
    connection.onError((error) => errors.push(error));
    const invalidate = vi.spyOn(queryClient, "invalidateQueries");
    send.mockRejectedValue(new Error("Validation failed"));

    connection.updateModule(withGain(3));
    await vi.advanceTimersByTimeAsync(80);

    expect(errors.map((error) => error.message)).toEqual(["Validation failed"]);
    expect(invalidate).toHaveBeenCalledWith({ queryKey: getGetModulesQueryKey() });
  });
});

describe("engine events", () => {
  const connectionA: ConnectionDto = {
    id: "connection-1",
    sourceModuleId: "a",
    sourcePort: "Output",
    targetModuleId: "b",
    targetPort: "Input",
  };
  const subgraph: SubgraphDto = {
    id: "subgraph-1",
    name: "Voice",
    position: { x: 0, y: 0 },
    size: { width: 240, height: 120 },
    color: "Blue",
    isCollapsed: false,
    isMuted: false,
    isBypassed: false,
  };

  test("no handler returns a value, which SignalR would send to the engine", () => {
    const events: [string, ...unknown[]][] = [
      ["ModuleChanged", original],
      ["ModuleRemoved", original.id],
      ["ConnectionAdded", connectionA],
      ["ConnectionRemoved", connectionA.id],
      ["SubgraphChanged", subgraph],
      ["SubgraphRemoved", subgraph.id],
      ["TemplatesChanged", []],
      ["DevicesChanged"],
      ["StatusChanged", {}],
      ["PreferencesChanged", {}],
      ["PluginsChanged", []],
      ["PortLayoutsChanged", []],
      ["ModuleData", original.id, {}],
      ["Levels", {}],
    ];

    expect([...hub().handlers.keys()].toSorted()).toEqual(events.map(([event]) => event).toSorted());
    for (const [event, ...args] of events) {
      expect(receive(event, ...args), event).toBeUndefined();
    }
  });

  test("a changed module replaces the cached one", () => {
    receive("ModuleChanged", withGain(6));

    expect(cachedGain()).toBe(6);
    expect(queryClient.getQueryData(getGetModuleQueryKey(original.id))).toEqual(withGain(6));
  });

  test("an event during a fetch of the same data starts the fetch again, which may have read older data", () => {
    void queryClient.fetchQuery({
      queryKey: getGetModulesQueryKey(),
      queryFn: () => new Promise<ModuleDto[]>(() => {}),
      // the cached modules are fresh
      staleTime: 0,
    });
    const invalidate = vi.spyOn(queryClient, "invalidateQueries");

    receive("ModuleChanged", withGain(6));

    expect(invalidate).toHaveBeenCalledWith({ queryKey: getGetModulesQueryKey(), exact: true });
    expect(invalidate).not.toHaveBeenCalledWith({ queryKey: getGetModuleQueryKey(original.id), exact: true });
  });

  test("a new module is added", () => {
    const other = testModule("Gain", { gain: 1 }, { id: "gain-2" });

    receive("ModuleChanged", other);

    expect(queryClient.getQueryData(getGetModulesQueryKey())).toEqual([original, other]);
  });

  test("a module change is ignored while a local update is pending", async () => {
    connection.updateModule(withGain(3));

    receive("ModuleChanged", withGain(-10));
    expect(cachedGain()).toBe(3);

    await vi.advanceTimersByTimeAsync(80);
    receive("ModuleChanged", withGain(-10));
    expect(cachedGain()).toBe(-10);
  });

  test("a removed module leaves the cache and its pending update isn't sent", async () => {
    queryClient.setQueryData(getGetModuleQueryKey(original.id), original);
    connection.updateModule(withGain(3));

    receive("ModuleRemoved", original.id);
    await vi.advanceTimersByTimeAsync(80);

    expect(queryClient.getQueryData(getGetModulesQueryKey())).toEqual([]);
    expect(queryClient.getQueryData(getGetModuleQueryKey(original.id))).toBeUndefined();
    expect(send).not.toHaveBeenCalled();
  });

  test("connections are added once and removed", () => {
    queryClient.setQueryData(getGetConnectionsQueryKey(), []);

    receive("ConnectionAdded", connectionA);
    receive("ConnectionAdded", connectionA);
    expect(queryClient.getQueryData(getGetConnectionsQueryKey())).toEqual([connectionA]);

    receive("ConnectionRemoved", connectionA.id);
    expect(queryClient.getQueryData(getGetConnectionsQueryKey())).toEqual([]);
  });

  test("changed port layouts replace the module's cached ones, and a removed module's are dropped", () => {
    const layouts = (moduleId: string, channelCount: number): ModulePortLayoutsDto => ({
      moduleId,
      inputs: { Input: { channelCount, speakers: null } },
      outputs: { Output: { channelCount, speakers: null } },
    });
    queryClient.setQueryData(getGetPortLayoutsQueryKey(), [layouts(original.id, 0), layouts("gain-2", 2)]);

    receive("PortLayoutsChanged", [layouts(original.id, 6), layouts("gain-3", 1)]);
    expect(queryClient.getQueryData(getGetPortLayoutsQueryKey())).toEqual([
      layouts(original.id, 6),
      layouts("gain-2", 2),
      layouts("gain-3", 1),
    ]);

    receive("ModuleRemoved", "gain-2");
    expect(queryClient.getQueryData(getGetPortLayoutsQueryKey())).toEqual([
      layouts(original.id, 6),
      layouts("gain-3", 1),
    ]);
  });

  test("events don't create lists that weren't loaded", () => {
    receive("ConnectionAdded", connectionA);
    receive("SubgraphChanged", subgraph);
    receive("PortLayoutsChanged", [{ moduleId: original.id, inputs: {}, outputs: {} }]);

    expect(queryClient.getQueryData(getGetConnectionsQueryKey())).toBeUndefined();
    expect(queryClient.getQueryData(getGetSubgraphsQueryKey())).toBeUndefined();
    expect(queryClient.getQueryData(getGetPortLayoutsQueryKey())).toBeUndefined();
  });

  test("a subgraph change is ignored while a local update is pending", async () => {
    queryClient.setQueryData(getGetSubgraphsQueryKey(), [subgraph]);
    connection.updateSubgraph({ ...subgraph, name: "Local" });

    receive("SubgraphChanged", { ...subgraph, name: "Engine" });
    expect(queryClient.getQueryData(getGetSubgraphsQueryKey())).toEqual([{ ...subgraph, name: "Local" }]);

    await vi.advanceTimersByTimeAsync(80);
    expect(sendSubgraph).toHaveBeenCalledWith(subgraph.id, { ...subgraph, name: "Local" });
  });

  test("a removed subgraph leaves the cache and its pending update isn't sent", async () => {
    queryClient.setQueryData(getGetSubgraphsQueryKey(), [subgraph]);
    connection.updateSubgraph({ ...subgraph, name: "Local" });

    receive("SubgraphRemoved", subgraph.id);
    await vi.advanceTimersByTimeAsync(80);

    expect(queryClient.getQueryData(getGetSubgraphsQueryKey())).toEqual([]);
    expect(sendSubgraph).not.toHaveBeenCalled();
  });

  test.each([
    ["TemplatesChanged", getGetSubgraphTemplatesQueryKey(), [{ id: "template-1" }]],
    ["StatusChanged", getGetEngineStatusQueryKey(), { isRunning: true }],
    ["PreferencesChanged", getGetPreferencesQueryKey(), { showStreamStatistics: true, snapToGrid: false }],
    ["PluginsChanged", getGetPluginsQueryKey(), [{ id: "Main" }]],
  ])("%s replaces the cached value", (event, queryKey, value) => {
    receive(event, value);

    expect(queryClient.getQueryData(queryKey)).toEqual(value);
  });

  test("changed devices reload every device list", () => {
    const invalidate = vi.spyOn(queryClient, "invalidateQueries");

    receive("DevicesChanged");

    expect(invalidate).toHaveBeenCalledWith({ queryKey: getGetDevicesQueryKey().slice(0, 1) });
  });
});

describe("live data", () => {
  test("goes to the module's subscribers only", () => {
    const listener = vi.fn();
    const otherListener = vi.fn();
    connection.subscribe("gain-1", listener);
    connection.subscribe("gain-2", otherListener);

    receive("ModuleData", "gain-1", { value: 1 });

    expect(listener).toHaveBeenCalledWith({ value: 1 });
    expect(otherListener).not.toHaveBeenCalled();
  });

  test("subscribes at the engine once per module while connected", async () => {
    connection.start();
    await vi.advanceTimersByTimeAsync(0);

    const unsubscribeFirst = connection.subscribe("gain-1", () => {});
    const unsubscribeSecond = connection.subscribe("gain-1", () => {});
    unsubscribeFirst();
    expect(hub().invoke.mock.calls).toEqual([["Subscribe", "gain-1"]]);

    unsubscribeSecond();
    expect(hub().invoke).toHaveBeenLastCalledWith("Unsubscribe", "gain-1");
  });

  test("levels are kept while subscribed", async () => {
    connection.start();
    await vi.advanceTimersByTimeAsync(0);
    const listener = vi.fn();
    const levels = { "gain-1": [{ port: "Output", peak: [0.5], rms: [0.2] }] };

    const unsubscribe = connection.subscribeLevels(listener);
    receive("Levels", levels);

    expect(hub().invoke).toHaveBeenCalledWith("SubscribeLevels");
    expect(listener).toHaveBeenCalledOnce();
    expect(connection.levels).toEqual(levels);

    unsubscribe();
    expect(connection.levels).toBeUndefined();
    expect(hub().invoke).toHaveBeenLastCalledWith("UnsubscribeLevels");
  });
});

describe("connection", () => {
  test("connecting reloads everything and renews the subscriptions", async () => {
    const states = recordStates();
    const invalidate = vi.spyOn(queryClient, "invalidateQueries");
    connection.subscribe("gain-1", () => {});
    connection.subscribeLevels(() => {});
    expect(hub().invoke).not.toHaveBeenCalled();

    connection.start();
    await vi.advanceTimersByTimeAsync(0);

    expect(states).toEqual(["connecting", "connected"]);
    expect(connection.state).toBe("connected");
    expect(invalidate).toHaveBeenCalledWith();
    expect(hub().invoke.mock.calls).toEqual([["Subscribe", "gain-1"], ["SubscribeLevels"]]);
  });

  test("a failed start is retried after 2 s", async () => {
    const states = recordStates();
    hub().start.mockRejectedValueOnce(new Error("No engine"));

    connection.start();
    await vi.advanceTimersByTimeAsync(1999);
    expect(states).toEqual(["connecting", "disconnected"]);
    expect(hub().start).toHaveBeenCalledOnce();

    await vi.advanceTimersByTimeAsync(1);
    expect(hub().start).toHaveBeenCalledTimes(2);
    expect(states.at(-1)).toBe("connected");
  });

  test("reports reconnecting and reloads after reconnecting", async () => {
    connection.start();
    await vi.advanceTimersByTimeAsync(0);
    const states = recordStates();
    const invalidate = vi.spyOn(queryClient, "invalidateQueries");

    hub().reconnecting!();
    hub().reconnected!();
    await vi.advanceTimersByTimeAsync(0);

    expect(states).toEqual(["reconnecting", "connected"]);
    expect(invalidate).toHaveBeenCalledOnce();
  });

  test("connecting cancels the fetches in progress before reloading, since they may have missed changes", async () => {
    const cancel = vi.spyOn(queryClient, "cancelQueries");
    const invalidate = vi.spyOn(queryClient, "invalidateQueries");

    connection.start();
    await vi.advanceTimersByTimeAsync(0);

    expect(cancel).toHaveBeenCalledBefore(invalidate);
  });

  test("starting while a stop is in progress connects once it's done, as when StrictMode restarts an effect", async () => {
    let rejectStart: (error: Error) => void = () => {};
    hub().start.mockImplementationOnce(() => {
      hub().state = HubConnectionState.Connecting;
      return new Promise((_, reject) => (rejectStart = reject));
    });
    hub().stop.mockImplementationOnce(async () => {
      hub().state = HubConnectionState.Disconnecting;
      rejectStart(new Error("Stopped during negotiation"));
      await Promise.resolve();
      hub().state = HubConnectionState.Disconnected;
    });
    const states = recordStates();

    connection.start();
    await vi.advanceTimersByTimeAsync(0);
    void connection.stop();
    connection.start();
    await vi.advanceTimersByTimeAsync(0);

    expect(hub().start).toHaveBeenCalledTimes(2);
    expect(states.at(-1)).toBe("connected");
  });

  test("a closed connection is restarted, but not after stopping", async () => {
    connection.start();
    await vi.advanceTimersByTimeAsync(0);

    hub().state = HubConnectionState.Disconnected;
    hub().close!();
    await vi.advanceTimersByTimeAsync(2000);
    expect(hub().start).toHaveBeenCalledTimes(2);

    await connection.stop();
    hub().close!();
    await vi.advanceTimersByTimeAsync(10_000);
    expect(hub().start).toHaveBeenCalledTimes(2);
  });
});
