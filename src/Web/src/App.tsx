import { FluentProvider, Toaster, makeStyles, tokens, webDarkTheme, webLightTheme } from "@fluentui/react-components";
import { EngineProvider } from "@micser/web-sdk";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ReactFlowProvider } from "@xyflow/react";
import { useState, useSyncExternalStore } from "react";
import { EngineToolbar } from "./components/EngineToolbar";
import { GraphEditor } from "./components/GraphEditor";
import { toasterId, useErrorNotifications } from "./notifications";

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

  return (
    <FluentProvider theme={prefersDark ? webDarkTheme : webLightTheme} style={{ height: "100%" }}>
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

  return (
    <ReactFlowProvider>
      <div className={styles.root}>
        <EngineToolbar />
        <GraphEditor />
      </div>
    </ReactFlowProvider>
  );
}
