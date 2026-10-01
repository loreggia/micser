import {
  Badge,
  Button,
  Caption1,
  Menu,
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
import { AddRegular, ArrowDownloadRegular, ArrowSyncRegular, SettingsRegular } from "@fluentui/react-icons";
import {
  getRestartAudioMutationOptions,
  getStartEngineMutationOptions,
  getStopEngineMutationOptions,
  useEngineConnectionState,
  useGetEngineStatus,
} from "@micser/web-sdk";
import { useMutation } from "@tanstack/react-query";
import { useState } from "react";
import { useNotifyError } from "../notifications";
import { shell, useShellState } from "../shell";
import { EngineSettingsDialog } from "./EngineSettingsDialog";
import { useAddModule, useModuleTypeChoices } from "./useAddModule";

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

const connectionBadges = {
  connected: { color: "success", text: "Connected" },
  connecting: { color: "informative", text: "Connecting" },
  reconnecting: { color: "warning", text: "Reconnecting" },
  disconnected: { color: "danger", text: "Disconnected" },
} as const;

export function EngineToolbar() {
  const styles = useStyles();
  const connectionState = useEngineConnectionState();
  const moduleTypes = useModuleTypeChoices();
  const { data: status } = useGetEngineStatus({ query: { refetchInterval: 2000 } });
  const shellState = useShellState();
  const notifyError = useNotifyError();
  const addModule = useAddModule();
  const [settingsOpen, setSettingsOpen] = useState(false);
  const start = useMutation({
    ...getStartEngineMutationOptions(),
    onError: (error) => notifyError("Starting the engine failed", error),
  });
  const stop = useMutation({
    ...getStopEngineMutationOptions(),
    onError: (error) => notifyError("Stopping the engine failed", error),
  });
  const restartAudio = useMutation({
    ...getRestartAudioMutationOptions(),
    onError: (error) => notifyError("Restarting the audio failed", error),
  });
  const badge = connectionBadges[connectionState];
  const isConnected = connectionState === "connected";

  return (
    <Toolbar className={styles.root}>
      <span className={styles.title}>Micser</span>
      <Menu>
        <MenuTrigger disableButtonEnhancement>
          <Button appearance="primary" icon={<AddRegular />} disabled={!isConnected}>
            Add module
          </Button>
        </MenuTrigger>
        <MenuPopover>
          <MenuList>
            {moduleTypes.map((type) => (
              <MenuItem key={type.type} onClick={() => void addModule(type.type)}>
                {type.title}
              </MenuItem>
            ))}
          </MenuList>
        </MenuPopover>
      </Menu>
      <div className={styles.spacer} />
      {shellState?.pendingUpdate && (
        <Tooltip content="Restarts Micser to install the update" relationship="description">
          <Button appearance="primary" icon={<ArrowDownloadRegular />} onClick={() => shell?.installUpdate()}>
            Update to {shellState.pendingUpdate}
          </Button>
        </Tooltip>
      )}
      {status && (
        <Caption1 className={styles.status}>
          {status.settings.sampleRate / 1000} kHz ·{" "}
          {((status.settings.frameCount / status.settings.sampleRate) * 1000).toFixed(1)} ms blocks
          {status.lateBlocks > 0 && ` · ${status.lateBlocks} late`}
        </Caption1>
      )}
      <Switch
        label="Audio"
        checked={status?.isRunning ?? false}
        disabled={!status || start.isPending || stop.isPending}
        onChange={(_, data) => (data.checked ? start.mutate() : stop.mutate())}
      />
      <Menu positioning="below-end">
        <MenuTrigger disableButtonEnhancement>
          {(triggerProps: MenuButtonProps) => (
            <SplitButton
              appearance="subtle"
              icon={<ArrowSyncRegular />}
              disabled={!isConnected || restartAudio.isPending}
              menuButton={triggerProps}
              primaryActionButton={{
                onClick: () => restartAudio.mutate(),
                title: "Restart the audio: reopens all devices with fresh buffers",
              }}
            >
              Restart
            </SplitButton>
          )}
        </MenuTrigger>
        <MenuPopover>
          <MenuList>
            <MenuItem onClick={() => restartAudio.mutate()}>Restart audio</MenuItem>
            <MenuItem disabled={!shellState?.canRestartEngine} onClick={() => shell?.restartEngine()}>
              Restart engine process
            </MenuItem>
          </MenuList>
        </MenuPopover>
      </Menu>
      <ToolbarDivider />
      <Badge appearance="tint" color={badge.color}>
        {badge.text}
      </Badge>
      <Tooltip content="Settings" relationship="label">
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
