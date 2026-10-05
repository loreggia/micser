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
import { useTranslation } from "../i18n";
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
 * Saves a subgraph as a template. The name starts as the subgraph's template's, so saving again updates it, or with " (custom)" added if
 * that's a built-in template. A name that a template has already replaces that template, unless it's a built-in one.
 */
function SaveTemplateDialog({ subgraph, onClose }: { subgraph: SubgraphDto; onClose: () => void }) {
  const styles = useStyles();
  const { t } = useTranslation();
  const { data: templates = [] } = useGetSubgraphTemplates();
  const notifyError = useNotifyError();
  const [name, setName] = useState(() => {
    const template = templates.find((other) => other.id === subgraph.templateId);
    return template
      ? template.isBuiltIn
        ? t("templates.save.customName", { name: template.name })
        : template.name
      : (subgraph.name ?? "");
  });
  const save = useMutation({
    ...getSaveSubgraphTemplateMutationOptions(),
    onSuccess: onClose,
    onError: (error) => notifyError(t("templates.save.failed"), error),
  });

  const trimmed = name.trim();
  const existing = templates.find(
    (other) => other.name.localeCompare(trimmed, undefined, { sensitivity: "accent" }) === 0
  );

  const isBuiltIn = existing?.isBuiltIn ?? false;

  const submit = () => {
    if (trimmed && !isBuiltIn) {
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
            <DialogTitle>{t("templates.save.title")}</DialogTitle>
            <DialogContent className={styles.content}>
              <Field
                label={t("templates.name")}
                validationState={isBuiltIn ? "error" : "none"}
                validationMessage={isBuiltIn ? t("templates.save.builtInName") : undefined}
                hint={
                  existing && !isBuiltIn
                    ? t("templates.save.replaces", { name: existing.name })
                    : t("templates.save.new")
                }
              >
                <Input autoFocus value={name} maxLength={maxNameLength} onChange={(_, data) => setName(data.value)} />
              </Field>
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" onClick={onClose}>
                {t("common.cancel")}
              </Button>
              <Button appearance="primary" type="submit" disabled={!trimmed || isBuiltIn || save.isPending}>
                {existing && !isBuiltIn ? t("templates.save.replace") : t("templates.save.save")}
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
  const { t } = useTranslation();
  const { data: templates = [] } = useGetSubgraphTemplates();
  const notifyError = useNotifyError();
  const template = templates.find((other) => other.id === subgraph.templateId);
  const update = useMutation({
    ...getUpdateSubgraphFromTemplateMutationOptions(),
    onSuccess: onClose,
    onError: (error) => notifyError(t("templates.update.failed"), error),
  });

  return (
    <Dialog open onOpenChange={(_, data) => !data.open && onClose()}>
      <DialogSurface>
        <DialogBody>
          <DialogTitle>{t("templates.update.title")}</DialogTitle>
          <DialogContent>
            {t("templates.update.description", {
              subgraph: subgraph.name || t("subgraph.fallbackName"),
              template: template?.name ?? "",
            })}
          </DialogContent>
          <DialogActions>
            <Button appearance="secondary" onClick={onClose}>
              {t("common.cancel")}
            </Button>
            <Button
              appearance="primary"
              disabled={!template || update.isPending}
              onClick={() => update.mutate({ id: subgraph.id })}
            >
              {t("templates.update.update")}
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
