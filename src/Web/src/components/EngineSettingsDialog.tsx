import {
  Button,
  Caption1,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  Divider,
  Dropdown,
  Field,
  Option,
  Subtitle2,
  Switch,
  makeStyles,
  tokens,
} from "@fluentui/react-components";
import { getUpdateEngineSettingsMutationOptions, usePreferences, type EngineSettingsDto } from "@micser/web-sdk";
import { useMutation } from "@tanstack/react-query";
import { useState } from "react";
import { useNotifyError } from "../notifications";
import { shell, useShellState, type UpdateCheckResult } from "../shell";

const sampleRates = [44100, 48000, 96000];
const frameCounts = [128, 240, 480, 960];

const checkResults: Record<UpdateCheckResult, string> = {
  upToDate: "Micser is up to date.",
  updateReady: "An update is ready to install.",
  failed: "Checking for updates failed.",
};

const useStyles = makeStyles({
  content: {
    display: "flex",
    flexDirection: "column",
    gap: tokens.spacingVerticalM,
  },
  updates: {
    display: "flex",
    alignItems: "center",
    flexWrap: "wrap",
    gap: tokens.spacingHorizontalM,
  },
  hint: {
    color: tokens.colorNeutralForeground3,
  },
});

export interface EngineSettingsDialogProps {
  open: boolean;
  settings: EngineSettingsDto;
  onClose: () => void;
}

/**
 * Audio settings (applied with Apply, which rebuilds the graph), display preferences (applied right away), and the version and updates
 * when running in the desktop shell.
 */
export function EngineSettingsDialog({ open, settings, onClose }: EngineSettingsDialogProps) {
  const styles = useStyles();
  const [draft, setDraft] = useState(settings);
  const [preferences, setPreferences] = usePreferences();
  const shellState = useShellState();
  const [checkResult, setCheckResult] = useState<UpdateCheckResult>();
  const notifyError = useNotifyError();
  const update = useMutation({
    ...getUpdateEngineSettingsMutationOptions(),
    onSuccess: onClose,
    onError: (error) => notifyError("Changing the engine settings failed", error),
  });

  const blockDuration = (frames: number) => `${frames} frames (${((frames / draft.sampleRate) * 1000).toFixed(1)} ms)`;
  const isChanged = draft.sampleRate !== settings.sampleRate || draft.frameCount !== settings.frameCount;

  return (
    <Dialog
      open={open}
      onOpenChange={(_, data) => {
        if (data.open) {
          setDraft(settings);
          setCheckResult(undefined);
        } else {
          onClose();
        }
      }}
    >
      <DialogSurface>
        <DialogBody>
          <DialogTitle>Settings</DialogTitle>
          <DialogContent className={styles.content}>
            <Subtitle2>Audio</Subtitle2>
            <Field label="Sample rate">
              <Dropdown
                value={`${draft.sampleRate / 1000} kHz`}
                selectedOptions={[String(draft.sampleRate)]}
                onOptionSelect={(_, data) => setDraft({ ...draft, sampleRate: Number(data.optionValue) })}
              >
                {sampleRates.map((rate) => (
                  <Option key={rate} value={String(rate)}>{`${rate / 1000} kHz`}</Option>
                ))}
              </Dropdown>
            </Field>
            <Field label="Block size" hint="Smaller blocks lower the latency and raise the CPU load.">
              <Dropdown
                value={blockDuration(draft.frameCount)}
                selectedOptions={[String(draft.frameCount)]}
                onOptionSelect={(_, data) => setDraft({ ...draft, frameCount: Number(data.optionValue) })}
              >
                {frameCounts.map((frames) => (
                  <Option key={frames} value={String(frames)}>
                    {blockDuration(frames)}
                  </Option>
                ))}
              </Dropdown>
            </Field>
            <Divider />
            <Subtitle2>Display</Subtitle2>
            <Switch
              label="Show stream statistics (dropouts and buffer size of device modules)"
              checked={preferences.showStreamStatistics}
              onChange={(_, data) => setPreferences({ showStreamStatistics: data.checked })}
            />
            <Switch
              label="Snap modules to the grid"
              checked={preferences.snapToGrid}
              onChange={(_, data) => setPreferences({ snapToGrid: data.checked })}
            />
            {shellState?.canUpdate && (
              <>
                <Divider />
                <Subtitle2>Micser {shellState.version}</Subtitle2>
                <div className={styles.updates}>
                  {shellState.pendingUpdate ? (
                    <Button appearance="primary" onClick={() => shell?.installUpdate()}>
                      Restart to update to {shellState.pendingUpdate}
                    </Button>
                  ) : (
                    <Button
                      disabled={shellState.isCheckingForUpdates}
                      onClick={() => void shell?.checkForUpdates().then(setCheckResult)}
                    >
                      {shellState.isCheckingForUpdates ? "Checking for updates…" : "Check for updates"}
                    </Button>
                  )}
                  {checkResult && !shellState.isCheckingForUpdates && (
                    <Caption1 className={styles.hint}>{checkResults[checkResult]}</Caption1>
                  )}
                </div>
              </>
            )}
          </DialogContent>
          <DialogActions>
            <Button appearance="secondary" onClick={onClose}>
              Close
            </Button>
            <Button
              appearance="primary"
              disabled={!isChanged || update.isPending}
              onClick={() => update.mutate({ data: draft })}
            >
              Apply audio settings
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
