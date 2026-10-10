import { Caption1, Dropdown, Option, makeStyles, tokens } from "@fluentui/react-components";
import {
  formatNumber,
  useGetDevices,
  useModuleData,
  usePreferences,
  type DeviceDirection,
  type ModuleOfType,
  type WidgetProps,
} from "@micser/web-sdk";
import { useTranslation } from "../i18n";

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
    minWidth: 0,
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
 * Selects the device of a device module and, if enabled in the preferences, shows the stream statistics.
 */
export function DeviceWidget({
  module,
  setState,
  direction,
}: WidgetProps<DeviceModuleType> & { direction: DeviceDirection }) {
  const styles = useStyles();
  const { t, language } = useTranslation();
  const { data: devices = [] } = useGetDevices({ direction, includeInactive: true });
  const [preferences] = usePreferences();
  const state = (module as ModuleOfType<DeviceModuleType>).state;

  const selected = devices.find((d) => d.id === state.deviceId);
  const label =
    selected?.name ??
    (state.deviceId
      ? state.adapterName
        ? t("device.unavailableAdapter", { adapter: state.adapterName })
        : t("device.unavailable")
      : t("device.none"));

  const availableDevices = devices
    .filter((d) => d.isActive || d.id === state.deviceId)
    .toSorted((a, b) => a.name.localeCompare(b.name, language, { numeric: true }));

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
        <Option value={none}>{t("device.none")}</Option>
        {availableDevices.map((device) => (
          <Option key={device.id} value={device.id} disabled={!device.isActive}>
            {device.isActive ? device.name : t("device.deviceUnavailable", { name: device.name })}
          </Option>
        ))}
      </Dropdown>
      {preferences.showStreamStatistics && state.deviceId && <StreamStatus moduleId={module.id} />}
    </div>
  );
}

/**
 * Dropouts and buffer size of the module's stream. Separate, so the statistics are only subscribed to while shown.
 */
function StreamStatus({ moduleId }: { moduleId: string }) {
  const styles = useStyles();
  const { t } = useTranslation();
  const statistics = useModuleData<StreamStatistics>(moduleId);
  if (!statistics) {
    return <Caption1 className={styles.warning}>{t("device.notRunning")}</Caption1>;
  }

  const dropouts = statistics.underruns + statistics.overruns + statistics.resyncs;
  return (
    <Caption1 className={dropouts > 0 ? styles.warning : styles.status}>
      {t("device.buffer", {
        status: dropouts > 0 ? t("device.dropouts", { count: dropouts }) : t("device.running"),
        milliseconds: formatNumber(statistics.targetMilliseconds, {
          minimumFractionDigits: 1,
          maximumFractionDigits: 1,
        }),
      })}
    </Caption1>
  );
}
