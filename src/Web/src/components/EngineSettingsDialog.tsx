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
import { ArrowSyncRegular } from "@fluentui/react-icons";
import {
  formatNumber,
  getRestartAudioMutationOptions,
  getUpdateEngineSettingsMutationOptions,
  languages,
  resolveLanguage,
  usePreferences,
  type EngineSettingsDto,
} from "@micser/web-sdk";
import { useMutation } from "@tanstack/react-query";
import { useState } from "react";
import { useTranslation } from "../i18n";
import { useNotifyError } from "../notifications";
import { shell, useShellState, type UpdateCheckResult } from "../shell";
import { PluginsSettings } from "./PluginsSettings";
import { ReleaseNotesDialog } from "./ReleaseNotesDialog";
import { VirtualCablesSettings } from "./VirtualCablesSettings";

const sampleRates = [44100, 48000, 96000];
const frameCounts = [128, 240, 480, 960];

const useStyles = makeStyles({
  content: {
    display: "flex",
    flexDirection: "column",
    gap: tokens.spacingVerticalM,
  },
  actions: {
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

/** The language preference's value that follows the system. */
const systemLanguage = "system";

/**
 * Audio settings (applied with Apply, which rebuilds the graph) and restarts, display preferences and the language (applied right away),
 * plugins (applied when the engine restarts), and, when running in the desktop shell, the version, updates and virtual
 * audio cables.
 */
export function EngineSettingsDialog({ open, settings, onClose }: EngineSettingsDialogProps) {
  const styles = useStyles();
  const { t } = useTranslation();
  const [draft, setDraft] = useState(settings);
  const [preferences, setPreferences] = usePreferences();
  const shellState = useShellState();
  const [checkResult, setCheckResult] = useState<UpdateCheckResult>();
  const [releaseNotesVersion, setReleaseNotesVersion] = useState<string>();
  const notifyError = useNotifyError();
  const update = useMutation({
    ...getUpdateEngineSettingsMutationOptions(),
    onSuccess: onClose,
    onError: (error) => notifyError(t("settings.changeFailed"), error),
  });
  const restartAudio = useMutation({
    ...getRestartAudioMutationOptions(),
    onError: (error) => notifyError(t("settings.restartAudioFailed"), error),
  });

  const blockDuration = (frames: number) =>
    t("settings.blockDuration", {
      frames,
      duration: formatNumber((frames / draft.sampleRate) * 1000, {
        minimumFractionDigits: 1,
        maximumFractionDigits: 1,
      }),
    });
  const sampleRate = (rate: number) => `${formatNumber(rate / 1000)} kHz`;
  const languageName = (code: string) => languages.find((language) => language.code === code)?.name ?? code;
  const systemLanguageName = t("settings.systemLanguage", { language: languageName(resolveLanguage(null)) });
  const languageValue =
    preferences.language && languages.some((language) => language.code === preferences.language)
      ? preferences.language
      : systemLanguage;
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
          <DialogTitle>{t("settings.title")}</DialogTitle>
          <DialogContent className={styles.content}>
            <Subtitle2>{t("settings.audio")}</Subtitle2>
            <Field label={t("settings.sampleRate")}>
              <Dropdown
                value={sampleRate(draft.sampleRate)}
                selectedOptions={[String(draft.sampleRate)]}
                onOptionSelect={(_, data) => setDraft({ ...draft, sampleRate: Number(data.optionValue) })}
              >
                {sampleRates.map((rate) => (
                  <Option key={rate} value={String(rate)}>
                    {sampleRate(rate)}
                  </Option>
                ))}
              </Dropdown>
            </Field>
            <Field label={t("settings.blockSize")} hint={t("settings.blockSizeHint")}>
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
            <Field hint={t("settings.restartAudioHint")}>
              <div className={styles.actions}>
                <Button
                  icon={<ArrowSyncRegular />}
                  disabled={restartAudio.isPending}
                  onClick={() => restartAudio.mutate()}
                >
                  {t("settings.restartAudio")}
                </Button>
                {shellState?.canRestartEngine && (
                  <Button onClick={() => shell?.restartEngine()}>{t("settings.restartEngine")}</Button>
                )}
              </div>
            </Field>
            <Divider />
            <Subtitle2>{t("settings.display")}</Subtitle2>
            <Field label={t("settings.language")}>
              <Dropdown
                value={languageValue === systemLanguage ? systemLanguageName : languageName(languageValue)}
                selectedOptions={[languageValue]}
                onOptionSelect={(_, data) =>
                  setPreferences({ language: data.optionValue === systemLanguage ? null : data.optionValue })
                }
              >
                <Option value={systemLanguage}>{systemLanguageName}</Option>
                {languages.map((language) => (
                  <Option key={language.code} value={language.code}>
                    {language.name}
                  </Option>
                ))}
              </Dropdown>
            </Field>
            <Switch
              label={t("settings.showStreamStatistics")}
              checked={preferences.showStreamStatistics}
              onChange={(_, data) => setPreferences({ showStreamStatistics: data.checked })}
            />
            <Switch
              label={t("settings.snapToGrid")}
              checked={preferences.snapToGrid}
              onChange={(_, data) => setPreferences({ snapToGrid: data.checked })}
            />
            <Divider />
            <PluginsSettings />
            {shellState?.canUpdate && (
              <>
                <Divider />
                <Subtitle2>Micser {shellState.version}</Subtitle2>
                <div className={styles.actions}>
                  {shellState.pendingUpdate ? (
                    <Button appearance="primary" onClick={() => shell?.installUpdate()}>
                      {t("settings.restartToUpdate", { version: shellState.pendingUpdate })}
                    </Button>
                  ) : (
                    <Button
                      disabled={shellState.isCheckingForUpdates}
                      onClick={() => void shell?.checkForUpdates().then(setCheckResult)}
                    >
                      {shellState.isCheckingForUpdates
                        ? t("settings.checkingForUpdates")
                        : t("settings.checkForUpdates")}
                    </Button>
                  )}
                  {checkResult && !shellState.isCheckingForUpdates && (
                    <Caption1 className={styles.hint}>{t(`settings.checkResults.${checkResult}`)}</Caption1>
                  )}
                </div>
                <div className={styles.actions}>
                  {shellState.version && (
                    <Button onClick={() => setReleaseNotesVersion(shellState.version!)}>
                      {t("settings.releaseNotes")}
                    </Button>
                  )}
                  {shellState.pendingUpdate && (
                    <Button onClick={() => setReleaseNotesVersion(shellState.pendingUpdate!)}>
                      {t("settings.whatsNewIn", { version: shellState.pendingUpdate })}
                    </Button>
                  )}
                </div>
                {releaseNotesVersion && (
                  <ReleaseNotesDialog
                    version={releaseNotesVersion}
                    open
                    onClose={() => setReleaseNotesVersion(undefined)}
                  />
                )}
              </>
            )}
            {shellState?.driver && (
              <>
                <Divider />
                <VirtualCablesSettings driver={shellState.driver} />
              </>
            )}
          </DialogContent>
          <DialogActions>
            <Button appearance="secondary" onClick={onClose}>
              {t("common.close")}
            </Button>
            <Button
              appearance="primary"
              disabled={!isChanged || update.isPending}
              onClick={() => update.mutate({ data: draft })}
            >
              {t("settings.applyAudioSettings")}
            </Button>
          </DialogActions>
        </DialogBody>
      </DialogSurface>
    </Dialog>
  );
}
