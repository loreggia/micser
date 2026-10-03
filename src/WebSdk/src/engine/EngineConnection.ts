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
  updateModule,
  type ConnectionDto,
  type EngineStatusDto,
  type ModuleDto,
  type PluginDto,
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

/** Delay before a module update is sent; later updates within it replace earlier ones. */
const updateDelay = 80;

/**
 * The SignalR connection to the engine. Engine events patch the query cache, so all clients stay in sync; module
 * updates are applied to the cache immediately and sent to the engine debounced.
 */
export class EngineConnection {
  private readonly dataListeners = new Map<string, Set<Listener<unknown>>>();
  private readonly errorListeners = new Set<Listener<Error>>();
  private readonly hub: HubConnection;
  private readonly levelListeners = new Set<() => void>();
  private readonly pendingUpdates = new Map<string, { module: ModuleDto; timer?: number; sending: boolean }>();
  private readonly queryClient: QueryClient;
  private readonly stateListeners = new Set<Listener<EngineConnectionState>>();
  private isStopped = true;
  private latestLevels?: ModuleLevels;
  private retryTimer?: number;

  constructor(queryClient: QueryClient) {
    this.queryClient = queryClient;
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
   * Called with errors of module updates, e.g. for a notification. Returns a function that removes the listener.
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
    void this.connect();
  }

  async stop() {
    this.isStopped = true;
    window.clearTimeout(this.retryTimer);
    await this.hub.stop();
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
    this.setModuleInCache(module);

    const pending = this.pendingUpdates.get(module.id) ?? { module, sending: false };
    pending.module = module;
    window.clearTimeout(pending.timer);
    pending.timer = window.setTimeout(() => {
      pending.timer = undefined;
      void this.sendUpdate(module.id);
    }, updateDelay);
    this.pendingUpdates.set(module.id, pending);
  }

  private async connect() {
    if (this.isStopped || this.hub.state !== HubConnectionState.Disconnected) {
      return;
    }

    this.setState("connecting");
    try {
      await this.hub.start();
      this.onConnected();
    } catch {
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
    if (!this.pendingUpdates.has(module.id)) {
      this.setModuleInCache(module);
    }
  }

  private onModuleRemoved(moduleId: string) {
    this.pendingUpdates.delete(moduleId);
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

  private async sendUpdate(moduleId: string) {
    const pending = this.pendingUpdates.get(moduleId);
    if (!pending || pending.sending) {
      return;
    }

    const module = pending.module;
    pending.sending = true;

    try {
      const updated = await updateModule(moduleId, module);
      if (pending.module === module) {
        this.pendingUpdates.delete(moduleId);
        this.setModuleInCache(updated);
      }
    } catch (error) {
      if (pending.module === module) {
        this.pendingUpdates.delete(moduleId);
        void this.queryClient.invalidateQueries({ queryKey: getGetModulesQueryKey() });
      }

      this.errorListeners.forEach((listener) => listener(error instanceof Error ? error : new Error(String(error))));
    } finally {
      pending.sending = false;
      if (this.pendingUpdates.get(moduleId) === pending && pending.module !== module && pending.timer === undefined) {
        // changed while sending and the delay has passed already
        void this.sendUpdate(moduleId);
      }
    }
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

  private setState(state: EngineConnectionState) {
    this.stateListeners.forEach((listener) => listener(state));
  }
}
