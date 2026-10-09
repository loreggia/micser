import {
  FluentProvider,
  Toaster,
  createDarkTheme,
  createLightTheme,
  makeStyles,
  tokens,
} from "@fluentui/react-components";
import { EngineProvider, useLanguagePreference } from "@micser/web-sdk";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ReactFlowProvider } from "@xyflow/react";
import { useMemo, useState, useSyncExternalStore } from "react";
import { EngineToolbar } from "./components/EngineToolbar";
import { GraphEditor } from "./components/GraphEditor";
import { ModuleActionsProvider } from "./components/ModuleActionsProvider";
import { SubgraphActionsProvider } from "./components/SubgraphActionsProvider";
import { toasterId, useErrorNotifications } from "./notifications";
import { PluginsProvider } from "./PluginsProvider";
import { useShellLanguage } from "./shell";
import { brandVariants } from "./theme";

const useStyles = makeStyles({
  root: {
    display: "grid",
    gridTemplateRows: "auto minmax(0, 1fr)",
    height: "100%",
    overflow: "hidden",
    backgroundColor: tokens.colorNeutralBackground2,
  },
});

const darkScheme = window.matchMedia("(prefers-color-scheme: dark)");

function usePrefersDark() {
  return useSyncExternalStore(
    (onChange) => {
      darkScheme.addEventListener("change", onChange);
      return () => darkScheme.removeEventListener("change", onChange);
    },
    () => darkScheme.matches
  );
}

export function App() {
  const prefersDark = usePrefersDark();
  // the engine pushes every change, so cached data never goes stale
  const [queryClient] = useState(
    () => new QueryClient({ defaultOptions: { queries: { staleTime: Infinity, refetchOnWindowFocus: false } } })
  );

  const theme = useMemo(
    () => (prefersDark ? createDarkTheme(brandVariants) : createLightTheme(brandVariants)),
    [prefersDark]
  );

  return (
    <FluentProvider theme={theme} style={{ height: "100%" }}>
      <QueryClientProvider client={queryClient}>
        <EngineProvider>
          <Shell />
        </EngineProvider>
      </QueryClientProvider>
      <Toaster toasterId={toasterId} position="bottom-end" />
    </FluentProvider>
  );
}

function Shell() {
  const styles = useStyles();
  useErrorNotifications();
  useLanguagePreference();
  useShellLanguage();

  return (
    <PluginsProvider>
      <ReactFlowProvider>
        <SubgraphActionsProvider>
          <ModuleActionsProvider>
            <div className={styles.root}>
              <EngineToolbar />
              <GraphEditor />
            </div>
          </ModuleActionsProvider>
        </SubgraphActionsProvider>
      </ReactFlowProvider>
    </PluginsProvider>
  );
}
