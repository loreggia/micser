import { useContext, useEffect, useState, useSyncExternalStore } from "react";
import type { ModuleDto } from "../api";
import type { EngineConnectionState } from "./EngineConnection";
import { EngineConnectionContext } from "./EngineContext";

export function useEngineConnection() {
  const connection = useContext(EngineConnectionContext);
  if (!connection) {
    throw new Error("useEngineConnection must be used inside an EngineProvider.");
  }

  return connection;
}

export function useEngineConnectionState(): EngineConnectionState {
  const connection = useEngineConnection();
  return useSyncExternalStore(
    (onChange) => connection.onStateChanged(onChange),
    () => connection.state
  );
}

/**
 * Returns the latest live data of a module (see the engine's IModuleDataSource), or undefined until the first push.
 */
export function useModuleData<T>(moduleId: string): T | undefined {
  const connection = useEngineConnection();
  const [latest, setLatest] = useState<{ moduleId: string; data: T }>();

  useEffect(
    () => connection.subscribe(moduleId, (data) => setLatest({ moduleId, data: data as T })),
    [connection, moduleId]
  );

  return latest?.moduleId === moduleId ? latest.data : undefined;
}

/**
 * Returns a function that replaces a module: applied immediately, sent to the engine debounced.
 */
export function useModuleUpdate(): (module: ModuleDto) => void {
  const connection = useEngineConnection();
  return (module) => connection.updateModule(module);
}
