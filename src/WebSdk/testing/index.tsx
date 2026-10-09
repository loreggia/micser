import { FluentProvider, webLightTheme } from "@fluentui/react-components";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useState, type ReactNode } from "react";
import { i18n, type Language } from "../src/i18n/i18n";
import { EngineConnection } from "../src/engine/EngineConnection";
import { EngineConnectionContext } from "../src/engine/EngineContext";
import type { ModuleOfType, ModuleType } from "../src/plugin";

/**
 * A query client for tests: data never goes stale and failed queries aren't retried. Seed it with `setQueryData` under the generated
 * query keys, so hooks don't fetch.
 */
export function createTestQueryClient() {
  return new QueryClient({ defaultOptions: { queries: { staleTime: Infinity, retry: false } } });
}

/**
 * The providers widgets need: the Fluent UI theme, the query client and an engine connection. The default connection is never started,
 * so nothing is sent to an engine. The UI is shown in the language, English by default.
 */
export function TestProviders({
  queryClient,
  connection,
  language = "en",
  children,
}: {
  queryClient: QueryClient;
  connection?: EngineConnection;
  language?: Language;
  children: ReactNode;
}) {
  const [defaultConnection] = useState(() => connection ?? new EngineConnection(queryClient));
  if (i18n.language !== language) {
    // the resources are loaded, so this changes the language synchronously
    void i18n.changeLanguage(language);
  }

  return (
    <FluentProvider theme={webLightTheme}>
      <QueryClientProvider client={queryClient}>
        <EngineConnectionContext.Provider value={connection ?? defaultConnection}>
          {children}
        </EngineConnectionContext.Provider>
      </QueryClientProvider>
    </FluentProvider>
  );
}

/**
 * A module of the type with the state, not muted, bypassed or collapsed, at full volume.
 */
export function testModule<T extends ModuleType>(
  type: T,
  state: ModuleOfType<T>["state"],
  overrides?: Partial<ModuleOfType<T>>
): ModuleOfType<T> {
  return {
    id: `${type}-1`,
    type,
    state,
    isBypassed: false,
    isCollapsed: false,
    isMuted: false,
    showChannels: false,
    useSystemVolume: false,
    volume: 1,
    ...overrides,
  } as ModuleOfType<T>;
}
