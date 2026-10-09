import {
  Button,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  Field,
  SpinButton,
} from "@fluentui/react-components";
import { useGetConnections, useGetModules, useModuleUpdate, type ModuleDto } from "@micser/web-sdk";
import { useMemo, useState, type ReactNode } from "react";
import { useTranslation } from "../i18n";
import { maxChannelCount, requiredChannelCount } from "./channels";
import { ModuleActionsContext, type ModuleActions } from "./moduleActions";

/** Provides {@link ModuleActions} and hosts their dialogs. */
export function ModuleActionsProvider({ children }: { children: ReactNode }) {
  const [choosingChannelCount, setChoosingChannelCount] = useState<string>();

  const actions = useMemo<ModuleActions>(
    () => ({ chooseChannelCount: (module) => setChoosingChannelCount(module.id) }),
    []
  );

  return (
    <ModuleActionsContext.Provider value={actions}>
      {children}
      {choosingChannelCount && (
        <ChannelCountDialog moduleId={choosingChannelCount} onClose={() => setChoosingChannelCount(undefined)} />
      )}
    </ModuleActionsContext.Provider>
  );
}

/** Sets a module's channel count, from the channels its connections to single channels need up to 64. */
function ChannelCountDialog({ moduleId, onClose }: { moduleId: string; onClose: () => void }) {
  const { t } = useTranslation();
  const { data: modules } = useGetModules();
  const { data: connections = [] } = useGetConnections();
  const update = useModuleUpdate();
  const module = modules?.find((m) => m.id === moduleId);
  const minimum = Math.max(1, requiredChannelCount(moduleId, connections));
  const [count, setCount] = useState(() => Math.max(module?.channelCount ?? 2, minimum));

  const submit = () => {
    if (module) {
      update({ ...module, channelCount: count } as ModuleDto);
    }

    onClose();
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
            <DialogTitle>{t("channels.customTitle")}</DialogTitle>
            <DialogContent>
              <Field
                label={t("channels.count")}
                hint={minimum > 1 ? t("channels.minimum", { count: minimum }) : undefined}
              >
                <SpinButton
                  autoFocus
                  min={minimum}
                  max={maxChannelCount}
                  value={count}
                  onChange={(_, data) => {
                    const value = data.value ?? Number(data.displayValue);
                    if (Number.isInteger(value)) {
                      setCount(Math.min(Math.max(value, minimum), maxChannelCount));
                    }
                  }}
                />
              </Field>
            </DialogContent>
            <DialogActions>
              <Button appearance="secondary" onClick={onClose}>
                {t("common.cancel")}
              </Button>
              <Button appearance="primary" type="submit" disabled={!module}>
                {t("channels.apply")}
              </Button>
            </DialogActions>
          </DialogBody>
        </form>
      </DialogSurface>
    </Dialog>
  );
}
