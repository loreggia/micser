import { Button, Caption1, Subtitle2, makeStyles, tokens } from "@fluentui/react-components";
import { exportConfiguration, getImportConfigurationMutationOptions } from "@micser/web-sdk";
import { useMutation } from "@tanstack/react-query";
import { useRef, useState } from "react";
import { useTranslation } from "../i18n";
import { useNotifyError } from "../notifications";

const useStyles = makeStyles({
  row: {
    display: "flex",
    alignItems: "center",
    flexWrap: "wrap",
    gap: tokens.spacingHorizontalM,
  },
  hint: {
    color: tokens.colorNeutralForeground3,
  },
});

/** The export's file name, e.g. "micser-config-2026-10-11.json". */
function exportFileName(date = new Date()) {
  const pad = (value: number) => String(value).padStart(2, "0");
  return `micser-config-${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}.json`;
}

function download(data: unknown, fileName: string) {
  const url = URL.createObjectURL(new Blob([JSON.stringify(data, null, 2)], { type: "application/json" }));
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  link.click();
  URL.revokeObjectURL(url);
}

export interface ConfigurationSettingsProps {
  /** Called after an import replaced the configuration. */
  onImported: () => void;
}

/**
 * Exports the whole configuration (graph, templates, settings, preferences) as a JSON file and replaces it with an imported one.
 */
export function ConfigurationSettings({ onImported }: ConfigurationSettingsProps) {
  const styles = useStyles();
  const { t } = useTranslation();
  const notifyError = useNotifyError();
  const fileInput = useRef<HTMLInputElement>(null);
  const [pendingFile, setPendingFile] = useState<File>();
  const exportMutation = useMutation({
    mutationFn: () => exportConfiguration(),
    onSuccess: (data) => download(data, exportFileName()),
    onError: (error) => notifyError(t("configuration.exportFailed"), error),
  });
  const importMutation = useMutation({
    ...getImportConfigurationMutationOptions(),
    onSuccess: () => {
      setPendingFile(undefined);
      onImported();
    },
    onError: (error) => notifyError(t("configuration.importFailed"), error),
  });

  return (
    <>
      <Subtitle2>{t("configuration.title")}</Subtitle2>
      <Caption1 className={styles.hint}>{t("configuration.description")}</Caption1>
      {pendingFile ? (
        <div className={styles.row}>
          <Caption1>{t("configuration.confirmImport", { file: pendingFile.name })}</Caption1>
          <Button disabled={importMutation.isPending} onClick={() => setPendingFile(undefined)}>
            {t("common.cancel")}
          </Button>
          <Button
            appearance="primary"
            disabled={importMutation.isPending}
            onClick={() => importMutation.mutate({ data: { file: pendingFile } })}
          >
            {importMutation.isPending ? t("configuration.importing") : t("configuration.replace")}
          </Button>
        </div>
      ) : (
        <div className={styles.row}>
          <Button disabled={exportMutation.isPending} onClick={() => exportMutation.mutate()}>
            {t("configuration.export")}
          </Button>
          <Button onClick={() => fileInput.current?.click()}>{t("configuration.import")}</Button>
        </div>
      )}
      <input
        ref={fileInput}
        type="file"
        accept=".json,application/json"
        hidden
        onChange={(event) => {
          const file = event.target.files?.[0];
          event.target.value = "";
          setPendingFile(file);
        }}
      />
    </>
  );
}
