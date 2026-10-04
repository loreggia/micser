import {
  Badge,
  Body1,
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
 * subgraphs created from a template.
 */
export function SubgraphTemplatesDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const styles = useStyles();
  const { data: templates = [] } = useGetSubgraphTemplates();

  return (
    <Dialog open={open} onOpenChange={(_, data) => !data.open && onClose()}>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>Subgraph templates</DialogTitle>
          <DialogContent className={styles.list}>
            {templates.length === 0 ? (
              <Body1 className={styles.empty}>
                No templates yet. Save a subgraph as a template from the menu in its header.
              </Body1>
            ) : (
              [...templates]
                .sort((a, b) => a.name.localeCompare(b.name))
                .map((template) => <TemplateRow key={template.id} template={template} />)
            )}
          </DialogContent>
          <DialogActions>
            <Button appearance="primary" onClick={onClose}>
              Close
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}

function TemplateRow({ template }: { template: SubgraphTemplateDto }) {
  const styles = useStyles();
  const { data: subgraphs = [] } = useGetSubgraphs();
  const notifyError = useNotifyError();
  const [confirmingDelete, setConfirmingDelete] = useState(false);
  const rename = useMutation({
    ...getRenameSubgraphTemplateMutationOptions(),
    onError: (error) => notifyError("Renaming the template failed", error),
  });
  const remove = useMutation({
    ...getDeleteSubgraphTemplateMutationOptions(),
    onError: (error) => notifyError("Removing the template failed", error),
  });

  const moduleCount = template.modules.length + template.unavailableTypes.length;
  const usedBy = subgraphs.filter((s) => s.templateId === template.id).length;
  const missingTypes = [...new Set(template.unavailableTypes)];

  return (
    <div className={styles.row}>
      <div>
        <ModuleTitle
          name={template.name}
          fallback={template.name}
          label="Template name"
          onRename={(name) => name && rename.mutate({ id: template.id, data: { name } })}
        />
        <div className={styles.details}>
          <Caption1>
            {moduleCount === 1 ? "1 module" : `${moduleCount} modules`} ·{" "}
            {usedBy === 0 ? "not used" : usedBy === 1 ? "used by 1 subgraph" : `used by ${usedBy} subgraphs`}
          </Caption1>
          {missingTypes.length > 0 && (
            <Tooltip content={`Plugins not loaded for: ${missingTypes.join(", ")}`} relationship="description">
              <Badge appearance="tint" color="warning">
                Unavailable
              </Badge>
            </Tooltip>
          )}
        </div>
      </div>
      <div className={styles.actions}>
        {confirmingDelete ? (
          <>
            <Caption1>Remove?</Caption1>
            <Button size="small" onClick={() => setConfirmingDelete(false)}>
              Cancel
            </Button>
            <Button
              size="small"
              appearance="primary"
              disabled={remove.isPending}
              onClick={() => remove.mutate({ id: template.id })}
            >
              Remove
            </Button>
          </>
        ) : (
          <Tooltip content="Remove (subgraphs created from it stay)" relationship="label">
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
