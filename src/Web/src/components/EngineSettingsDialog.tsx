import {
  Button,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  Dropdown,
  Field,
  Option,
  makeStyles,
  tokens,
} from "@fluentui/react-components";
import { getUpdateEngineSettingsMutationOptions, type EngineSettingsDto } from "@micser/web-sdk";
import { useMutation } from "@tanstack/react-query";
import { useState } from "react";
import { useNotifyError } from "../notifications";

const sampleRates = [44100, 48000, 96000];
const frameCounts = [128, 240, 480, 960];

const useStyles = makeStyles({
  content: {
    display: "flex",
    flexDirection: "column",
    gap: tokens.spacingVerticalM,
  },
});

export interface EngineSettingsDialogProps {
  open: boolean;
  settings: EngineSettingsDto;
  onClose: () => void;
}

export function EngineSettingsDialog({ open, settings, onClose }: EngineSettingsDialogProps) {
  const styles = useStyles();
  const [draft, setDraft] = useState(settings);
  const notifyError = useNotifyError();
  const update = useMutation({
    ...getUpdateEngineSettingsMutationOptions(),
    onSuccess: onClose,
    onError: (error) => notifyError("Changing the engine settings failed", error),
  });

  const blockDuration = (frames: number) => `${frames} frames (${((frames / draft.sampleRate) * 1000).toFixed(1)} ms)`;

  return (
    <Dialog
      open={open}
      onOpenChange={(_, data) => {
        if (data.open) {
          setDraft(settings);
        } else {
          onClose();
        }
      }}
    >
      <DialogSurface>
        <DialogBody>
          <DialogTitle>Engine settings</DialogTitle>
          <DialogContent className={styles.content}>
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
          </DialogContent>
          <DialogActions>
            <Button appearance="secondary" onClick={onClose}>
              Cancel
            </Button>
            <Button appearance="primary" disabled={update.isPending} onClick={() => update.mutate({ data: draft })}>
              Apply
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
