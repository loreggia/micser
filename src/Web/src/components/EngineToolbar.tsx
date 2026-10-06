import {
  Badge,
  Button,
  Caption1,
  Menu,
  MenuDivider,
  MenuItem,
  MenuList,
  MenuPopover,
  MenuTrigger,
  SplitButton,
  Switch,
  Toolbar,
  ToolbarDivider,
  Tooltip,
  makeStyles,
  tokens,
  type MenuButtonProps,
} from "@fluentui/react-components";
import { AddRegular } from "@fluentui/react-icons/svg/add";
import { ArrowDownloadRegular } from "@fluentui/react-icons/svg/arrow-download";
import { SettingsRegular } from "@fluentui/react-icons/svg/settings";
import {
  formatNumber,
  getStartEngineMutationOptions,
  getStopEngineMutationOptions,
  useEngineConnectionState,
  useGetEngineStatus,
} from "@micser/web-sdk";
import { useMutation } from "@tanstack/react-query";
import { useState } from "react";
import { useTranslation } from "../i18n";
import { useNotifyError } from "../notifications";
import { shell, useShellState } from "../shell";
import { EngineSettingsDialog } from "./EngineSettingsDialog";
import { ReleaseNotesDialog } from "./ReleaseNotesDialog";
import { TemplatesSubmenu } from "./TemplatesSubmenu";
import { useAddModule, useInstantiateTemplate, useModuleTypeChoices } from "./useAddModule";

const useStyles = makeStyles({
  root: {
    backgroundColor: tokens.colorNeutralBackground1,
    borderBottom: `${tokens.strokeWidthThin} solid ${tokens.colorNeutralStroke2}`,
    paddingInline: tokens.spacingHorizontalM,
    gap: tokens.spacingHorizontalS,
  },
  title: {
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase400,
    marginRight: tokens.spacingHorizontalM,
  },
  spacer: {
    flexGrow: 1,
  },
  status: {
    color: tokens.colorNeutralForeground3,
    fontVariantNumeric: "tabular-nums",
  },
});

const connectionColors = {
  connected: "success",
  connecting: "informative",
  reconnecting: "warning",
  disconnected: "danger",
} as const;

export function EngineToolbar() {
  const styles = useStyles();
  const { t } = useTranslation();
  const connectionState = useEngineConnectionState();
  const moduleTypes = useModuleTypeChoices();
  const { data: status } = useGetEngineStatus({ query: { refetchInterval: 2000 } });
  const shellState = useShellState();
  const notifyError = useNotifyError();
  const addModule = useAddModule();
  const instantiateTemplate = useInstantiateTemplate();
  const [settingsOpen, setSettingsOpen] = useState(false);
  const [releaseNotesOpen, setReleaseNotesOpen] = useState(false);
  const start = useMutation({
    ...getStartEngineMutationOptions(),
    onError: (error) => notifyError(t("toolbar.startFailed"), error),
  });
  const stop = useMutation({
    ...getStopEngineMutationOptions(),
    onError: (error) => notifyError(t("toolbar.stopFailed"), error),
  });
  const isConnected = connectionState === "connected";

  return (
    <Toolbar className={styles.root}>
      <span className={styles.title}>Micser</span>
      <Menu>
        <MenuTrigger disableButtonEnhancement>
          <Button appearance="primary" icon={<AddRegular />} disabled={!isConnected}>
            {t("toolbar.addModule")}
          </Button>
        </MenuTrigger>
        <MenuPopover>
          <MenuList>
            {moduleTypes.map((type) => (
              <MenuItem key={type.type} onClick={() => void addModule(type.type)}>
                {type.title}
              </MenuItem>
            ))}
            <MenuDivider />
            <TemplatesSubmenu onInstantiate={(templateId) => instantiateTemplate(templateId)} />
          </MenuList>
        </MenuPopover>
      </Menu>
      <div className={styles.spacer} />
      {shellState?.pendingUpdate && (
        <>
          <Menu positioning="below-end">
            <MenuTrigger disableButtonEnhancement>
              {(triggerProps: MenuButtonProps) => (
                <Tooltip content={t("toolbar.updateHint")} relationship="description">
                  <SplitButton
                    appearance="primary"
                    icon={<ArrowDownloadRegular />}
                    menuButton={{ ...triggerProps, "aria-label": t("toolbar.updateOptions") }}
                    primaryActionButton={{ onClick: () => shell?.installUpdate() }}
                  >
                    {t("toolbar.updateTo", { version: shellState.pendingUpdate })}
                  </SplitButton>
                </Tooltip>
              )}
            </MenuTrigger>
            <MenuPopover>
              <MenuList>
                <MenuItem onClick={() => setReleaseNotesOpen(true)}>{t("toolbar.whatsNew")}</MenuItem>
              </MenuList>
            </MenuPopover>
          </Menu>
          <ReleaseNotesDialog
            version={shellState.pendingUpdate}
            open={releaseNotesOpen}
            onClose={() => setReleaseNotesOpen(false)}
          />
        </>
      )}
      {status && (
        <Caption1 className={styles.status}>
          {t("toolbar.status", {
            sampleRate: formatNumber(status.settings.sampleRate / 1000),
            blockDuration: formatNumber((status.settings.frameCount / status.settings.sampleRate) * 1000, {
              minimumFractionDigits: 1,
              maximumFractionDigits: 1,
            }),
          })}
          {status.lateBlocks > 0 && t("toolbar.lateBlocks", { count: status.lateBlocks })}
        </Caption1>
      )}
      <Switch
        label={t("toolbar.audio")}
        checked={status?.isRunning ?? false}
        disabled={!status || start.isPending || stop.isPending}
        onChange={(_, data) => (data.checked ? start.mutate() : stop.mutate())}
      />
      <ToolbarDivider />
      <Badge appearance="tint" color={connectionColors[connectionState]}>
        {t(`toolbar.connection.${connectionState}`)}
      </Badge>
      <Tooltip content={t("toolbar.settings")} relationship="label">
        <Button
          appearance="subtle"
          icon={<SettingsRegular />}
          disabled={!status}
          onClick={() => setSettingsOpen(true)}
        />
      </Tooltip>
      {status && (
        <EngineSettingsDialog open={settingsOpen} settings={status.settings} onClose={() => setSettingsOpen(false)} />
      )}
    </Toolbar>
  );
}
