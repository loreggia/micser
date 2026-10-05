import { useGetPlugins } from "@micser/web-sdk";
import { useEffect, useRef, useState, type ReactNode } from "react";
import { useTranslation } from "./i18n";
import { useNotifyError } from "./notifications";
import { loadPluginWidgets, PluginWidgetsContext, type PluginWidgets } from "./plugins";

/**
 * Loads the widgets of the engine's plugins. Plugins only change when the engine restarts; the widgets of plugins that are new then are
 * loaded too.
 */
export function PluginsProvider({ children }: { children: ReactNode }) {
  const { data: plugins } = useGetPlugins();
  const notifyError = useNotifyError();
  const { t } = useTranslation();
  const [value, setValue] = useState<PluginWidgets>({ widgets: new Map(), isLoading: true });
  const notified = useRef(new Set<string>());

  useEffect(() => {
    if (!plugins) {
      return;
    }

    let isCurrent = true;
    void loadPluginWidgets(plugins).then(({ widgets, failures }) => {
      if (!isCurrent) {
        return;
      }

      for (const { plugin, error } of failures) {
        if (!notified.current.has(plugin.id)) {
          notified.current.add(plugin.id);
          notifyError(t("plugins.widgetsFailed", { name: plugin.name ?? plugin.id }), error);
        }
      }

      setValue({ widgets, isLoading: false });
    });

    return () => {
      isCurrent = false;
    };
  }, [plugins, notifyError, t]);

  return <PluginWidgetsContext.Provider value={value}>{children}</PluginWidgetsContext.Provider>;
}
