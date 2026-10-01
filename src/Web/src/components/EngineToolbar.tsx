import {
  Badge,
  Button,
  Caption1,
  Menu,
  MenuItem,
  MenuList,
  MenuPopover,
  MenuTrigger,
  Switch,
  Toolbar,
  ToolbarDivider,
  Tooltip,
  makeStyles,
  tokens,
} from "@fluentui/react-components";
import { AddRegular, SettingsRegular } from "@fluentui/react-icons";
import {
  getStartEngineMutationOptions,
  getStopEngineMutationOptions,
  useEngineConnectionState,
  useGetEngineStatus,
  useGetModuleTypes,
} from "@micser/web-sdk";
import { useMutation } from "@tanstack/react-query";
import { useState } from "react";
import { useNotifyError } from "../notifications";
import { widgets } from "../plugins";
import { EngineSettingsDialog } from "./EngineSettingsDialog";
import { useAddModule } from "./useAddModule";

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
  const { data: moduleTypes = [] } = useGetModuleTypes();
  const { data: status } = useGetEngineStatus({ query: { refetchInterval: 2000 } });
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
  const badge = connectionBadges[connectionState];

  const sortedTypes = moduleTypes
    .map((type) => ({ type: type.type, title: widgets.get(type.type)?.title ?? type.type }))
    .sort((a, b) => a.title.localeCompare(b.title));

  return (
    <Toolbar className={styles.root}>
      <span className={styles.title}>Micser</span>
      <Menu>
        <MenuTrigger disableButtonEnhancement>
          <Button appearance="primary" icon={<AddRegular />} disabled={connectionState !== "connected"}>
            Add module
          </Button>
        </MenuTrigger>
        <MenuPopover>
          <MenuList>
            {sortedTypes.map((type) => (
              <MenuItem key={type.type} onClick={() => addModule(type.type)}>
                {type.title}
              </MenuItem>
            ))}
          </MenuList>
        </MenuPopover>
      </Menu>
      <div className={styles.spacer} />
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
      <ToolbarDivider />
      <Badge appearance="tint" color={badge.color}>
        {badge.text}
      </Badge>
      <Tooltip content="Engine settings" relationship="label">
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
