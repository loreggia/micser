import { Badge, Button, Caption1, Subtitle2, Text, makeStyles, tokens } from "@fluentui/react-components";
import { DeleteRegular } from "@fluentui/react-icons";
import {
  getInstallPluginMutationOptions,
  getRemovePluginMutationOptions,
  useGetPlugins,
  type PluginDto,
} from "@micser/web-sdk";
import { useMutation } from "@tanstack/react-query";
import { useRef } from "react";
import { useNotifyError } from "../notifications";
import { shell, useShellState } from "../shell";

const useStyles = makeStyles({
  list: {
    display: "flex",
    flexDirection: "column",
    gap: tokens.spacingVerticalS,
  },
  plugin: {
    display: "grid",
    gridTemplateColumns: "minmax(0, 1fr) auto",
    alignItems: "center",
    columnGap: tokens.spacingHorizontalM,
  },
  title: {
    display: "flex",
    alignItems: "center",
    flexWrap: "wrap",
    gap: tokens.spacingHorizontalS,
  },
  row: {
    display: "flex",
    alignItems: "center",
    flexWrap: "wrap",
    gap: tokens.spacingHorizontalM,
  },
  hint: {
    color: tokens.colorNeutralForeground3,
  },
  error: {
    color: tokens.colorPaletteRedForeground1,
  },
});

function pluginStatus(plugin: PluginDto) {
  switch (plugin.pendingChange) {
    case "Install":
      return plugin.isLoaded ? "Updated when the engine restarts" : "Installed when the engine restarts";
    case "Remove":
      return "Removed when the engine restarts";
    default:
      return undefined;
  }
}

/**
 * Lists the engine's plugins, installs plugin packages (.zip) and removes user plugins. Changes take effect when the engine restarts.
 */
export function PluginsSettings() {
  const styles = useStyles();
  const { data: plugins = [] } = useGetPlugins();
  const shellState = useShellState();
  const notifyError = useNotifyError();
  const fileInput = useRef<HTMLInputElement>(null);
  const install = useMutation({
    ...getInstallPluginMutationOptions(),
    onError: (error) => notifyError("Installing the plugin failed", error),
  });
  const remove = useMutation({
    ...getRemovePluginMutationOptions(),
    onError: (error) => notifyError("Removing the plugin failed", error),
  });

  const hasPendingChanges = plugins.some((plugin) => plugin.pendingChange !== "None");

  return (
    <>
      <Subtitle2>Plugins</Subtitle2>
      <div className={styles.list}>
        {plugins.map((plugin) => (
          <div key={`${plugin.id}-${plugin.isBuiltIn}`} className={styles.plugin}>
            <div>
              <div className={styles.title}>
                <Text weight="semibold">{plugin.name ?? plugin.id}</Text>
                {plugin.version && <Caption1 className={styles.hint}>{plugin.version}</Caption1>}
                {plugin.isBuiltIn && (
                  <Badge appearance="tint" color="informative" size="small">
                    built in
                  </Badge>
                )}
              </div>
              {plugin.error && <Caption1 className={styles.error}>Not loaded: {plugin.error}</Caption1>}
              {pluginStatus(plugin) && <Caption1 className={styles.hint}>{pluginStatus(plugin)}</Caption1>}
            </div>
            {!plugin.isBuiltIn && plugin.pendingChange !== "Remove" && (
              <Button
                appearance="subtle"
                icon={<DeleteRegular />}
                aria-label={`Remove ${plugin.name ?? plugin.id}`}
                disabled={remove.isPending}
                onClick={() => remove.mutate({ id: plugin.id })}
              />
            )}
          </div>
        ))}
      </div>
      <Caption1 className={styles.hint}>
        Plugins run inside the engine with full access to your computer; only install plugins you trust.
      </Caption1>
      <div className={styles.row}>
        <Button disabled={install.isPending} onClick={() => fileInput.current?.click()}>
          {install.isPending ? "Installing…" : "Install plugin…"}
        </Button>
        {hasPendingChanges &&
          (shellState?.canRestartEngine ? (
            <Button appearance="primary" onClick={() => shell?.restartEngine()}>
              Restart engine to apply
            </Button>
          ) : (
            <Caption1 className={styles.hint}>Restart the engine to apply the changes.</Caption1>
          ))}
        <input
          ref={fileInput}
          type="file"
          accept=".zip"
          hidden
          onChange={(event) => {
            const file = event.target.files?.[0];
            event.target.value = "";
            if (file) {
              install.mutate({ data: { package: file } });
            }
          }}
        />
      </div>
    </>
  );
}
