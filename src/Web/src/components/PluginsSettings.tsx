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
import { useTranslation } from "../i18n";
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

function pluginStatus(plugin: PluginDto, t: ReturnType<typeof useTranslation>["t"]) {
  switch (plugin.pendingChange) {
    case "Install":
      return plugin.isLoaded ? t("plugins.updatedOnRestart") : t("plugins.installedOnRestart");
    case "Remove":
      return t("plugins.removedOnRestart");
    default:
      return undefined;
  }
}

/**
 * Lists the engine's plugins, installs plugin packages (.zip) and removes user plugins. Changes take effect when the engine restarts.
 */
export function PluginsSettings() {
  const styles = useStyles();
  const { t } = useTranslation();
  const { data: plugins = [] } = useGetPlugins();
  const shellState = useShellState();
  const notifyError = useNotifyError();
  const fileInput = useRef<HTMLInputElement>(null);
  const install = useMutation({
    ...getInstallPluginMutationOptions(),
    onError: (error) => notifyError(t("plugins.installFailed"), error),
  });
  const remove = useMutation({
    ...getRemovePluginMutationOptions(),
    onError: (error) => notifyError(t("plugins.removeFailed"), error),
  });

  const hasPendingChanges = plugins.some((plugin) => plugin.pendingChange !== "None");

  return (
    <>
      <Subtitle2>{t("plugins.title")}</Subtitle2>
      <div className={styles.list}>
        {plugins.map((plugin) => (
          <div key={`${plugin.id}-${plugin.isBuiltIn}`} className={styles.plugin}>
            <div>
              <div className={styles.title}>
                <Text weight="semibold">{plugin.name ?? plugin.id}</Text>
                {plugin.version && <Caption1 className={styles.hint}>{plugin.version}</Caption1>}
                {plugin.isBuiltIn && (
                  <Badge appearance="tint" color="informative" size="small">
                    {t("plugins.builtIn")}
                  </Badge>
                )}
              </div>
              {plugin.error && (
                <Caption1 className={styles.error}>{t("plugins.notLoaded", { error: plugin.error })}</Caption1>
              )}
              {pluginStatus(plugin, t) && <Caption1 className={styles.hint}>{pluginStatus(plugin, t)}</Caption1>}
            </div>
            {!plugin.isBuiltIn && plugin.pendingChange !== "Remove" && (
              <Button
                appearance="subtle"
                icon={<DeleteRegular />}
                aria-label={t("plugins.remove", { name: plugin.name ?? plugin.id })}
                disabled={remove.isPending}
                onClick={() => remove.mutate({ id: plugin.id })}
              />
            )}
          </div>
        ))}
      </div>
      <Caption1 className={styles.hint}>{t("plugins.trustHint")}</Caption1>
      <div className={styles.row}>
        <Button disabled={install.isPending} onClick={() => fileInput.current?.click()}>
          {install.isPending ? t("plugins.installing") : t("plugins.install")}
        </Button>
        {hasPendingChanges &&
          (shellState?.canRestartEngine ? (
            <Button appearance="primary" onClick={() => shell?.restartEngine()}>
              {t("plugins.restartToApply")}
            </Button>
          ) : (
            <Caption1 className={styles.hint}>{t("plugins.restartHint")}</Caption1>
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
