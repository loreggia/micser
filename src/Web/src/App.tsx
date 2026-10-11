import { FluentProvider, Toaster, makeStyles, tokens } from "@fluentui/react-components";
import { EngineProvider, useLanguagePreference } from "@micser/web-sdk";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ReactFlowProvider } from "@xyflow/react";
import { useEffect, useState } from "react";
import { EngineToolbar } from "./components/EngineToolbar";
import { GraphEditor } from "./components/GraphEditor";
import { ModuleActionsProvider } from "./components/ModuleActionsProvider";
import { SubgraphActionsProvider } from "./components/SubgraphActionsProvider";
import { toasterId, useErrorNotifications } from "./notifications";
import { PluginsProvider } from "./PluginsProvider";
import { useShellColorMode, useShellLanguage } from "./shell";
import { useTheme } from "./theme";

const useStyles = makeStyles({
  root: {
    display: "grid",
    gridTemplateRows: "auto minmax(0, 1fr)",
    height: "100%",
    overflow: "hidden",
    backgroundColor: tokens.colorNeutralBackground2,
  },
});

export function App() {
  // the engine pushes every change, so cached data never goes stale
  const [queryClient] = useState(
    () => new QueryClient({ defaultOptions: { queries: { staleTime: Infinity, refetchOnWindowFocus: false } } })
  );

  return (
    <QueryClientProvider client={queryClient}>
      <EngineProvider>
        <ThemeProvider />
      </EngineProvider>
    </QueryClientProvider>
  );
}

/** Applies the theme preference; until the preferences are loaded, the system's light or dark mode. */
function ThemeProvider() {
  const { theme, colorMode } = useTheme();

  useEffect(() => {
    // native controls such as scrollbars
    document.documentElement.style.colorScheme = colorMode;
  }, [colorMode]);

  return (
    <FluentProvider theme={theme} style={{ height: "100%" }}>
      <Shell />
      <Toaster toasterId={toasterId} position="bottom-end" />
    </FluentProvider>
  );
}

function Shell() {
  const styles = useStyles();
  useErrorNotifications();
  useLanguagePreference();
  useShellLanguage();
  useShellColorMode();

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
