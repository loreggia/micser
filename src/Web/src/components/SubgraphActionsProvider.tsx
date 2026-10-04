import {
  Button,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  Field,
  Input,
  makeStyles,
  tokens,
} from "@fluentui/react-components";
import {
  getSaveSubgraphTemplateMutationOptions,
  getUpdateSubgraphFromTemplateMutationOptions,
  useGetSubgraphTemplates,
  type SubgraphDto,
} from "@micser/web-sdk";
import { useMutation } from "@tanstack/react-query";
import { useMemo, useState, type ReactNode } from "react";
import { useNotifyError } from "../notifications";
import { SubgraphActionsContext, type SubgraphActions } from "./subgraphActions";
import { SubgraphTemplatesDialog } from "./SubgraphTemplatesDialog";

/** Longest template name the engine accepts. */
const maxNameLength = 100;

const useStyles = makeStyles({
  content: {
    display: "flex",
    flexDirection: "column",
    gap: tokens.spacingVerticalM,
  },
});

/**
 * Provides {@link SubgraphActions} and hosts their dialogs: saving a subgraph as a template, updating one from its template, and
 * managing the templates.
 */
export function SubgraphActionsProvider({ children }: { children: ReactNode }) {
  const [saving, setSaving] = useState<SubgraphDto>();
  const [updating, setUpdating] = useState<SubgraphDto>();
  const [managing, setManaging] = useState(false);

  const actions = useMemo<SubgraphActions>(
    () => ({
      manageTemplates: () => setManaging(true),
      saveAsTemplate: setSaving,
      updateFromTemplate: setUpdating,
    }),
    []
  );

  return (
    <SubgraphActionsContext.Provider value={actions}>
      {children}
      {saving && <SaveTemplateDialog subgraph={saving} onClose={() => setSaving(undefined)} />}
      {updating && <UpdateFromTemplateDialog subgraph={updating} onClose={() => setUpdating(undefined)} />}
      <SubgraphTemplatesDialog open={managing} onClose={() => setManaging(false)} />
    </SubgraphActionsContext.Provider>
  );
}

/**
 * Saves a subgraph as a template. The name starts as the subgraph's template's, so saving again updates it; a name that a template has
 * already replaces that template.
 */
function SaveTemplateDialog({ subgraph, onClose }: { subgraph: SubgraphDto; onClose: () => void }) {
  const styles = useStyles();
  const { data: templates = [] } = useGetSubgraphTemplates();
  const notifyError = useNotifyError();
  const [name, setName] = useState(
    () => templates.find((t) => t.id === subgraph.templateId)?.name ?? subgraph.name ?? ""
  );
  const save = useMutation({
    ...getSaveSubgraphTemplateMutationOptions(),
    onSuccess: onClose,
    onError: (error) => notifyError("Saving the template failed", error),
  });

  const trimmed = name.trim();
  const existing = templates.find((t) => t.name.localeCompare(trimmed, undefined, { sensitivity: "accent" }) === 0);

  const submit = () => {
    if (trimmed) {
      save.mutate({ data: { subgraphId: subgraph.id, name: trimmed, templateId: existing?.id ?? null } });
    }
  };

  return (
    <Dialog open onOpenChange={(_, data) => !data.open && onClose()}>
      <DialogSurface>
        <form
          onSubmit={(event) => {
            event.preventDefault();
            submit();
          }}
        >
          <DialogBody>
            <DialogTitle>Save as template</DialogTitle>
            <DialogContent className={styles.content}>
              <Field
                label="Template name"
                hint={
                  existing
                    ? `Replaces the template "${existing.name}". Subgraphs created from it can then be updated.`
                    : "Saves a new template."
                }
              >
                <Input autoFocus value={name} maxLength={maxNameLength} onChange={(_, data) => setName(data.value)} />
              </Field>
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" onClick={onClose}>
                Cancel
              </Button>
              <Button appearance="primary" type="submit" disabled={!trimmed || save.isPending}>
                {existing ? "Replace" : "Save"}
              </Button>
            </DialogActions>
          </DialogBody>
        </form>
      </DialogSurface>
    </Dialog>
  );
}

/** Confirms updating a subgraph from its template, which replaces its modules' settings and connections between them. */
function UpdateFromTemplateDialog({ subgraph, onClose }: { subgraph: SubgraphDto; onClose: () => void }) {
  const { data: templates = [] } = useGetSubgraphTemplates();
  const notifyError = useNotifyError();
  const template = templates.find((t) => t.id === subgraph.templateId);
  const update = useMutation({
    ...getUpdateSubgraphFromTemplateMutationOptions(),
    onSuccess: onClose,
    onError: (error) => notifyError("Updating from the template failed", error),
  });

  return (
    <Dialog open onOpenChange={(_, data) => !data.open && onClose()}>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>Update from template</DialogTitle>
          <DialogContent>
            The modules of &quot;{subgraph.name || "Subgraph"}&quot; take the settings of the template &quot;
            {template?.name}&quot;, modules the template doesn&apos;t have are removed, and its missing ones are added.
            Connections to modules outside the subgraph stay.
          </DialogContent>
          <DialogActions>
            <Button appearance="secondary" onClick={onClose}>
              Cancel
            </Button>
            <Button
              appearance="primary"
              disabled={!template || update.isPending}
              onClick={() => update.mutate({ id: subgraph.id })}
            >
              Update
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
