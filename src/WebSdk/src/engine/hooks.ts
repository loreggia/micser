import { useQueryClient } from "@tanstack/react-query";
import { useCallback, useContext, useEffect, useState, useSyncExternalStore } from "react";
import { i18n, resolveLanguage } from "../i18n/i18n";
import {
  getGetPreferencesQueryKey,
  updatePreferences,
  useGetPreferences,
  type ModuleDto,
  type SubgraphDto,
  type UiPreferencesDto,
} from "../api";
import type { EngineConnectionState, PortLevels } from "./EngineConnection";
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
 * Returns the levels of a module's ports, updated about 20 times per second, or undefined while the module isn't processed.
 */
export function useModuleLevels(moduleId: string): PortLevels[] | undefined {
  const connection = useEngineConnection();
  const subscribe = useCallback((onChange: () => void) => connection.subscribeLevels(onChange), [connection]);
  return useSyncExternalStore(subscribe, () => connection.levels?.[moduleId]);
}

const defaultPreferences: UiPreferencesDto = { showStreamStatistics: false, snapToGrid: true, language: null };

/**
 * Returns the UI preferences (defaults until loaded) and a function that changes some of them. Changes show immediately; the engine stores
 * them and pushes them to all clients.
 */
export function usePreferences(): [UiPreferencesDto, (changes: Partial<UiPreferencesDto>) => void] {
  const queryClient = useQueryClient();
  const { data: preferences = defaultPreferences } = useGetPreferences();

  const update = (changes: Partial<UiPreferencesDto>) => {
    const updated = { ...preferences, ...changes };
    queryClient.setQueryData(getGetPreferencesQueryKey(), updated);
    // on failure the cache is reloaded from the engine
    updatePreferences(updated).catch(() => queryClient.invalidateQueries({ queryKey: getGetPreferencesQueryKey() }));
  };

  return [preferences, update];
}

/**
 * Shows the UI in the language of the preferences (see `resolveLanguage`). Used once, at the root of the UI.
 */
export function useLanguagePreference() {
  const [preferences] = usePreferences();
  const language = resolveLanguage(preferences.language);

  useEffect(() => {
    void i18n.changeLanguage(language);
  }, [language]);
}

/**
 * Returns a function that replaces a module: applied immediately, sent to the engine debounced.
 */
export function useModuleUpdate(): (module: ModuleDto) => void {
  const connection = useEngineConnection();
  return (module) => connection.updateModule(module);
}

/**
 * Returns a function that replaces a subgraph: applied immediately, sent to the engine debounced.
 */
export function useSubgraphUpdate(): (subgraph: SubgraphDto) => void {
  const connection = useEngineConnection();
  return (subgraph) => connection.updateSubgraph(subgraph);
}
