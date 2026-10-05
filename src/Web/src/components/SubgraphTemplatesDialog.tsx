import {
  Badge,
  Body1,
  Body1Strong,
  Button,
  Caption1,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  Tooltip,
  makeStyles,
  tokens,
} from "@fluentui/react-components";
import { DeleteRegular } from "@fluentui/react-icons";
import {
  getDeleteSubgraphTemplateMutationOptions,
  getRenameSubgraphTemplateMutationOptions,
  useGetSubgraphs,
  useGetSubgraphTemplates,
  type SubgraphTemplateDto,
} from "@micser/web-sdk";
import { useMutation } from "@tanstack/react-query";
import { useState } from "react";
import { useTranslation } from "../i18n";
import { useNotifyError } from "../notifications";
import { ModuleTitle } from "./ModuleTitle";

const useStyles = makeStyles({
  list: {
    display: "flex",
    flexDirection: "column",
    gap: tokens.spacingVerticalS,
  },
  row: {
    display: "grid",
    gridTemplateColumns: "1fr auto",
    alignItems: "center",
    gap: tokens.spacingHorizontalM,
    paddingBlock: tokens.spacingVerticalXS,
    borderBottom: `${tokens.strokeWidthThin} solid ${tokens.colorNeutralStroke2}`,
  },
  details: {
    display: "flex",
    alignItems: "center",
    flexWrap: "wrap",
    gap: tokens.spacingHorizontalS,
    color: tokens.colorNeutralForeground3,
  },
  actions: {
    display: "flex",
    alignItems: "center",
    gap: tokens.spacingHorizontalS,
  },
  empty: {
    color: tokens.colorNeutralForeground3,
  },
});

/**
 * The subgraph templates: their modules and the subgraphs using them, renaming (double-click the name) and removing, which keeps the
 * subgraphs created from a template. Built-in templates come with the plugins and can't be renamed or removed.
 */
export function SubgraphTemplatesDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const styles = useStyles();
  const { t, language } = useTranslation();
  const { data: templates = [] } = useGetSubgraphTemplates();

  return (
    <Dialog open={open} onOpenChange={(_, data) => !data.open && onClose()}>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>{t("templates.title")}</DialogTitle>
          <DialogContent className={styles.list}>
            {templates.length === 0 ? (
              <Body1 className={styles.empty}>{t("templates.empty")}</Body1>
            ) : (
              [...templates]
                .sort((a, b) => a.name.localeCompare(b.name, language))
                .map((template) => <TemplateRow key={template.id} template={template} />)
            )}
          </DialogContent>
          <DialogActions>
            <Button appearance="primary" onClick={onClose}>
              {t("common.close")}
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}

function TemplateRow({ template }: { template: SubgraphTemplateDto }) {
  const styles = useStyles();
  const { t } = useTranslation();
  const { data: subgraphs = [] } = useGetSubgraphs();
  const notifyError = useNotifyError();
  const [confirmingDelete, setConfirmingDelete] = useState(false);
  const rename = useMutation({
    ...getRenameSubgraphTemplateMutationOptions(),
    onError: (error) => notifyError(t("templates.renameFailed"), error),
  });
  const remove = useMutation({
    ...getDeleteSubgraphTemplateMutationOptions(),
    onError: (error) => notifyError(t("templates.removeFailed"), error),
  });

  const moduleCount = template.modules.length + template.unavailableTypes.length;
  const usedBy = subgraphs.filter((s) => s.templateId === template.id).length;
  const missingTypes = [...new Set(template.unavailableTypes)];

  return (
    <div className={styles.row}>
      <div>
        {template.isBuiltIn ? (
          <Body1Strong>{template.name}</Body1Strong>
        ) : (
          <ModuleTitle
            name={template.name}
            fallback={template.name}
            label={t("templates.name")}
            onRename={(name) => name && rename.mutate({ id: template.id, data: { name } })}
          />
        )}
        <div className={styles.details}>
          <Caption1>
            {t("templates.modules", { count: moduleCount })} ·{" "}
            {usedBy === 0 ? t("templates.notUsed") : t("templates.usedBy", { count: usedBy })}
          </Caption1>
          {template.isBuiltIn && (
            <Tooltip content={t("templates.builtInHint")} relationship="description">
              <Badge appearance="tint">{t("templates.builtIn")}</Badge>
            </Tooltip>
          )}
          {missingTypes.length > 0 && (
            <Tooltip
              content={t("common.pluginsNotLoaded", { types: missingTypes.join(", ") })}
              relationship="description"
            >
              <Badge appearance="tint" color="warning">
                {t("templates.unavailable")}
              </Badge>
            </Tooltip>
          )}
        </div>
      </div>
      <div className={styles.actions}>
        {template.isBuiltIn ? null : confirmingDelete ? (
          <>
            <Caption1>{t("templates.confirmRemove")}</Caption1>
            <Button size="small" onClick={() => setConfirmingDelete(false)}>
              {t("common.cancel")}
            </Button>
            <Button
              size="small"
              appearance="primary"
              disabled={remove.isPending}
              onClick={() => remove.mutate({ id: template.id })}
            >
              {t("templates.remove")}
            </Button>
          </>
        ) : (
          <Tooltip content={t("templates.removeHint")} relationship="label">
            <Button
              size="small"
              appearance="subtle"
              icon={<DeleteRegular />}
              onClick={() => setConfirmingDelete(true)}
            />
          </Tooltip>
        )}
      </div>
    </div>
  );
}
