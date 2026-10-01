import { Caption1, Dropdown, Option, makeStyles, tokens } from "@fluentui/react-components";
import {
  useGetDevices,
  useModuleData,
  type DeviceDirection,
  type ModuleOfType,
  type WidgetProps,
} from "@micser/web-sdk";

interface StreamStatistics {
  fill: number;
  targetFill: number;
  correction: number;
  underruns: number;
  overruns: number;
  targetMilliseconds: number;
  resyncs: number;
}

const useStyles = makeStyles({
  root: {
    display: "flex",
    flexDirection: "column",
    gap: tokens.spacingVerticalXS,
  },
  dropdown: {
    minWidth: "220px",
  },
  status: {
    color: tokens.colorNeutralForeground3,
  },
  warning: {
    color: tokens.colorPaletteYellowForeground2,
  },
});

const none = "none";

type DeviceModuleType = "DeviceInput" | "LoopbackInput" | "DeviceOutput";

/**
 * Selects the device of a device module and shows the stream health.
 */
export function DeviceWidget({
  module,
  setState,
  direction,
}: WidgetProps<DeviceModuleType> & { direction: DeviceDirection }) {
  const styles = useStyles();
  const { data: devices = [] } = useGetDevices({ direction, includeInactive: true });
  const statistics = useModuleData<StreamStatistics>(module.id);
  const state = (module as ModuleOfType<DeviceModuleType>).state;

  const selected = devices.find((d) => d.id === state.deviceId);
  const label =
    selected?.name ??
    (state.deviceId ? `Unavailable${state.adapterName ? ` (${state.adapterName})` : ""}` : "No device");
  const dropouts = statistics ? statistics.underruns + statistics.overruns + statistics.resyncs : 0;

  return (
    <div className={styles.root}>
      <Dropdown
        className={styles.dropdown}
        size="small"
        value={label}
        selectedOptions={[state.deviceId ?? none]}
        onOptionSelect={(_, data) => {
          const device = devices.find((d) => d.id === data.optionValue);
          // a newly selected device learns its buffer from scratch
          setState({
            deviceId: device?.id ?? null,
            adapterName: device?.adapterName ?? null,
            bufferMilliseconds: null,
          });
        }}
      >
        <Option value={none}>No device</Option>
        {devices.map((device) => (
          <Option key={device.id} value={device.id} disabled={!device.isActive}>
            {device.isActive ? device.name : `${device.name} (unavailable)`}
          </Option>
        ))}
      </Dropdown>
      {statistics ? (
        <Caption1 className={dropouts > 0 ? styles.warning : styles.status}>
          {dropouts > 0 ? `${dropouts} dropouts` : "Running"} · {statistics.targetMilliseconds.toFixed(1)} ms buffer
        </Caption1>
      ) : (
        state.deviceId && <Caption1 className={styles.warning}>Not running</Caption1>
      )}
    </div>
  );
}
