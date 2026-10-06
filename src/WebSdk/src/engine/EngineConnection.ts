import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from "@microsoft/signalr";
import type { QueryClient } from "@tanstack/react-query";
import {
  getAccessToken,
  getGetConnectionsQueryKey,
  getGetDevicesQueryKey,
  getGetEngineStatusQueryKey,
  getGetModuleQueryKey,
  getGetModulesQueryKey,
  getGetPluginsQueryKey,
  getGetPreferencesQueryKey,
  getGetSubgraphsQueryKey,
  getGetSubgraphTemplatesQueryKey,
  updateModule,
  updateSubgraph,
  type ConnectionDto,
  type EngineStatusDto,
  type ModuleDto,
  type PluginDto,
  type SubgraphDto,
  type SubgraphTemplateDto,
  type UiPreferencesDto,
} from "../api";

export type EngineConnectionState = "connecting" | "connected" | "reconnecting" | "disconnected";

/**
 * The levels of one port of a module, per channel as linear amplitude (1 = full scale). Mirrors the engine's PortLevelsDto, which is pushed
 * by the hub and not part of the OpenAPI document.
 */
export interface PortLevels {
  /** The output port, or null for the signal a module without outputs passes on (e.g. what a device output plays). */
  port: string | null;
  /** The highest absolute sample value since the previous push. */
  peak: number[];
  /** The RMS level, smoothed over about 300 ms. */
  rms: number[];
}

/** The levels of all processed modules by module id; modules that aren't processed are missing. */
export type ModuleLevels = Record<string, PortLevels[]>;

type HubMethod = "Subscribe" | "Unsubscribe" | "SubscribeLevels" | "UnsubscribeLevels";

type Listener<T> = (value: T) => void;

/** Delay before an update is sent; later updates within it replace earlier ones. */
const updateDelay = 80;

/**
 * Updates of one kind of engine resource: applied to the cache right away and sent to the engine debounced, the last value
 * winning. The engine's answer to the last update is applied when it arrives.
 */
class PendingUpdates<T extends { id: string }> {
  private readonly onFailed: (error: unknown) => void;
  private readonly pending = new Map<string, { value: T; timer?: number; sending: boolean }>();
  private readonly send: (value: T) => Promise<T>;
  private readonly setInCache: (value: T) => void;

  constructor(send: (value: T) => Promise<T>, setInCache: (value: T) => void, onFailed: (error: unknown) => void) {
    this.send = send;
    this.setInCache = setInCache;
    this.onFailed = onFailed;
  }

  delete(id: string) {
    this.pending.delete(id);
  }

  has(id: string) {
    return this.pending.has(id);
  }

  update(value: T) {
    this.setInCache(value);

    const pending = this.pending.get(value.id) ?? { value, sending: false };
    pending.value = value;
    window.clearTimeout(pending.timer);
    pending.timer = window.setTimeout(() => {
      pending.timer = undefined;
      void this.sendPending(value.id);
    }, updateDelay);
    this.pending.set(value.id, pending);
  }

  private async sendPending(id: string) {
    const pending = this.pending.get(id);
    if (!pending || pending.sending) {
      return;
    }

    const value = pending.value;
    pending.sending = true;

    try {
      const updated = await this.send(value);
      if (pending.value === value) {
        this.pending.delete(id);
        this.setInCache(updated);
      }
    } catch (error) {
      if (pending.value === value) {
        this.pending.delete(id);
      }

      this.onFailed(error);
    } finally {
      pending.sending = false;
      if (this.pending.get(id) === pending && pending.value !== value && pending.timer === undefined) {
        // changed while sending and the delay has passed already
        void this.sendPending(id);
      }
    }
  }
}

/**
 * The SignalR connection to the engine. Engine events patch the query cache, so all clients stay in sync; module
 * updates are applied to the cache immediately and sent to the engine debounced.
 */
export class EngineConnection {
  private readonly dataListeners = new Map<string, Set<Listener<unknown>>>();
  private readonly errorListeners = new Set<Listener<Error>>();
  private readonly hub: HubConnection;
  private readonly levelListeners = new Set<() => void>();
  private readonly moduleUpdates: PendingUpdates<ModuleDto>;
  private readonly queryClient: QueryClient;
  private readonly stateListeners = new Set<Listener<EngineConnectionState>>();
  private readonly subgraphUpdates: PendingUpdates<SubgraphDto>;
  private isStopped = true;
  private latestLevels?: ModuleLevels;
  private retryTimer?: number;
  private stopping = Promise.resolve();

  constructor(queryClient: QueryClient) {
    this.queryClient = queryClient;
    this.moduleUpdates = new PendingUpdates(
      (module) => updateModule(module.id, module),
      (module) => this.setModuleInCache(module),
      (error) => this.onUpdateFailed(getGetModulesQueryKey(), error)
    );
    this.subgraphUpdates = new PendingUpdates(
      (subgraph) => updateSubgraph(subgraph.id, subgraph),
      (subgraph) => this.setSubgraphInCache(subgraph),
      (error) => this.onUpdateFailed(getGetSubgraphsQueryKey(), error)
    );
    this.hub = new HubConnectionBuilder()
      .withUrl("/hubs/engine", { accessTokenFactory: () => getAccessToken() ?? "" })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (context) => Math.min(500 * 2 ** context.previousRetryCount, 5000),
      })
      .configureLogging(LogLevel.Warning)
      .build();

    this.hub.on("ModuleChanged", (module: ModuleDto) => this.onModuleChanged(module));
    this.hub.on("ModuleRemoved", (moduleId: string) => this.onModuleRemoved(moduleId));
    // handlers must not return a value: SignalR would send it to the server as an invocation result
    this.hub.on("ConnectionAdded", (connection: ConnectionDto) => {
      this.queryClient.setQueryData<ConnectionDto[]>(getGetConnectionsQueryKey(), (connections) =>
        connections && !connections.some((c) => c.id === connection.id) ? [...connections, connection] : connections
      );
    });
    this.hub.on("ConnectionRemoved", (connectionId: string) => {
      this.queryClient.setQueryData<ConnectionDto[]>(getGetConnectionsQueryKey(), (connections) =>
        connections?.filter((c) => c.id !== connectionId)
      );
    });
    this.hub.on("SubgraphChanged", (subgraph: SubgraphDto) => {
      if (!this.subgraphUpdates.has(subgraph.id)) {
        this.setSubgraphInCache(subgraph);
      }
    });
    this.hub.on("SubgraphRemoved", (subgraphId: string) => {
      this.subgraphUpdates.delete(subgraphId);
      this.queryClient.setQueryData<SubgraphDto[]>(getGetSubgraphsQueryKey(), (subgraphs) =>
        subgraphs?.filter((s) => s.id !== subgraphId)
      );
    });
    this.hub.on("TemplatesChanged", (templates: SubgraphTemplateDto[]) => {
      this.queryClient.setQueryData(getGetSubgraphTemplatesQueryKey(), templates);
    });
    this.hub.on("DevicesChanged", () => {
      void this.queryClient.invalidateQueries({ queryKey: getGetDevicesQueryKey().slice(0, 1) });
    });
    this.hub.on("StatusChanged", (status: EngineStatusDto) => {
      this.queryClient.setQueryData(getGetEngineStatusQueryKey(), status);
    });
    this.hub.on("PreferencesChanged", (preferences: UiPreferencesDto) => {
      this.queryClient.setQueryData(getGetPreferencesQueryKey(), preferences);
    });
    this.hub.on("PluginsChanged", (plugins: PluginDto[]) => {
      this.queryClient.setQueryData(getGetPluginsQueryKey(), plugins);
    });
    this.hub.on("ModuleData", (moduleId: string, data: unknown) => {
      this.dataListeners.get(moduleId)?.forEach((listener) => listener(data));
    });
    this.hub.on("Levels", (levels: ModuleLevels) => {
      this.latestLevels = levels;
      this.levelListeners.forEach((listener) => listener());
    });

    this.hub.onreconnecting(() => this.setState("reconnecting"));
    this.hub.onreconnected(() => this.onConnected());
    this.hub.onclose(() => {
      this.setState("disconnected");
      this.scheduleRetry();
    });
  }

  /** The latest levels while subscribed with {@link subscribeLevels}. */
  get levels(): ModuleLevels | undefined {
    return this.latestLevels;
  }

  get state(): EngineConnectionState {
    switch (this.hub.state) {
      case HubConnectionState.Connected:
        return "connected";
      case HubConnectionState.Reconnecting:
        return "reconnecting";
      case HubConnectionState.Disconnected:
        return "disconnected";
      default:
        return "connecting";
    }
  }

  /**
   * Called with errors of module and subgraph updates, e.g. for a notification. Returns a function that removes the listener.
   */
  onError(listener: Listener<Error>) {
    this.errorListeners.add(listener);
    return () => {
      this.errorListeners.delete(listener);
    };
  }

  onStateChanged(listener: Listener<EngineConnectionState>) {
    this.stateListeners.add(listener);
    return () => {
      this.stateListeners.delete(listener);
    };
  }

  start() {
    this.isStopped = false;
    // after a stop that is still in progress, e.g. when React's StrictMode stops and restarts the effect
    void this.stopping.then(() => this.connect());
  }

  async stop() {
    this.isStopped = true;
    window.clearTimeout(this.retryTimer);
    const stopping = this.hub.stop();
    this.stopping = stopping.catch(() => undefined);
    await stopping;
  }

  /**
   * Receives the live data of a module while subscribed. Returns a function that ends the subscription.
   */
  subscribe(moduleId: string, listener: Listener<unknown>) {
    let listeners = this.dataListeners.get(moduleId);
    if (!listeners) {
      this.dataListeners.set(moduleId, (listeners = new Set()));
      this.invoke("Subscribe", moduleId);
    }

    listeners.add(listener);

    return () => {
      listeners.delete(listener);
      if (listeners.size === 0 && this.dataListeners.get(moduleId) === listeners) {
        this.dataListeners.delete(moduleId);
        this.invoke("Unsubscribe", moduleId);
      }
    };
  }

  /**
   * Calls the listener whenever {@link levels} changes (about 20 times per second) while subscribed. Returns a function that ends the
   * subscription.
   */
  subscribeLevels(listener: () => void) {
    this.levelListeners.add(listener);
    if (this.levelListeners.size === 1) {
      this.invoke("SubscribeLevels");
    }

    return () => {
      if (this.levelListeners.delete(listener) && this.levelListeners.size === 0) {
        this.latestLevels = undefined;
        this.invoke("UnsubscribeLevels");
      }
    };
  }

  /**
   * Shows the module in the cache right away and sends it to the engine after a short delay.
   */
  updateModule(module: ModuleDto) {
    this.moduleUpdates.update(module);
  }

  /**
   * Shows the subgraph in the cache right away and sends it to the engine after a short delay.
   */
  updateSubgraph(subgraph: SubgraphDto) {
    this.subgraphUpdates.update(subgraph);
  }

  private async connect() {
    if (this.isStopped || this.hub.state !== HubConnectionState.Disconnected) {
      return;
    }

    const stopping = this.stopping;
    this.setState("connecting");
    try {
      await this.hub.start();
      this.onConnected();
    } catch {
      // a stop cancelled the attempt; start() connects again
      if (this.stopping !== stopping) {
        return;
      }

      if (this.hub.state === HubConnectionState.Disconnected) {
        this.setState("disconnected");
        this.scheduleRetry();
      }
    }
  }

  private invoke(method: HubMethod, ...args: unknown[]) {
    if (this.hub.state === HubConnectionState.Connected) {
      this.hub.invoke(method, ...args).catch(() => {
        // resubscribed on the next reconnect
      });
    }
  }

  private onConnected() {
    window.clearTimeout(this.retryTimer);
    this.setState("connected");
    // events may have been missed while disconnected
    void this.queryClient.invalidateQueries();
    for (const moduleId of this.dataListeners.keys()) {
      this.invoke("Subscribe", moduleId);
    }

    if (this.levelListeners.size > 0) {
      this.invoke("SubscribeLevels");
    }
  }

  private onModuleChanged(module: ModuleDto) {
    // local edits win until they are sent; the engine's answer to the last one is applied then
    if (!this.moduleUpdates.has(module.id)) {
      this.setModuleInCache(module);
    }
  }

  private onModuleRemoved(moduleId: string) {
    this.moduleUpdates.delete(moduleId);
    this.queryClient.setQueryData<ModuleDto[]>(getGetModulesQueryKey(), (modules) =>
      modules?.filter((m) => m.id !== moduleId)
    );
    this.queryClient.removeQueries({ queryKey: getGetModuleQueryKey(moduleId) });
  }

  private scheduleRetry() {
    if (!this.isStopped) {
      window.clearTimeout(this.retryTimer);
      this.retryTimer = window.setTimeout(() => void this.connect(), 2000);
    }
  }

  private onUpdateFailed(queryKey: readonly unknown[], error: unknown) {
    void this.queryClient.invalidateQueries({ queryKey });
    this.errorListeners.forEach((listener) => listener(error instanceof Error ? error : new Error(String(error))));
  }

  private setModuleInCache(module: ModuleDto) {
    this.queryClient.setQueryData<ModuleDto[]>(getGetModulesQueryKey(), (modules) => {
      if (!modules) {
        return modules;
      }

      const index = modules.findIndex((m) => m.id === module.id);
      return index < 0 ? [...modules, module] : modules.with(index, module);
    });
    this.queryClient.setQueryData(getGetModuleQueryKey(module.id), module);
  }

  private setSubgraphInCache(subgraph: SubgraphDto) {
    this.queryClient.setQueryData<SubgraphDto[]>(getGetSubgraphsQueryKey(), (subgraphs) => {
      if (!subgraphs) {
        return subgraphs;
      }

      const index = subgraphs.findIndex((s) => s.id === subgraph.id);
      return index < 0 ? [...subgraphs, subgraph] : subgraphs.with(index, subgraph);
    });
  }

  private setState(state: EngineConnectionState) {
    this.stateListeners.forEach((listener) => listener(state));
  }
}
