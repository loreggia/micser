import { Dropdown, mergeClasses, Option, Switch } from "@fluentui/react-components";
import {
  formatNumber,
  InfoTip,
  ParameterSlider,
  useDefaultStyles,
  type StereoMode,
  type WidgetProps,
} from "@micser/web-sdk";
import { useTranslation } from "../i18n";

const modes: StereoMode[] = ["Stereo", "Swap", "LeftToBoth", "RightToBoth", "Mono"];

const percent = (value: number) => formatNumber(value, { style: "percent", maximumFractionDigits: 0 });

export function StereoToolsWidget({ module, setState }: WidgetProps<"StereoTools">) {
  const styles = useDefaultStyles();
  const { t } = useTranslation();
  const state = module.state;
  const modeLabel = (mode: StereoMode) => t(`stereoTools.modes.${mode}`);
  const balanceLabel = (balance: number) =>
    balance === 0
      ? t("stereoTools.center")
      : t(balance < 0 ? "stereoTools.left" : "stereoTools.right", { value: percent(Math.abs(balance)) });

  return (
    <div className={styles.column}>
      <div className={styles.withInfo}>
        <Dropdown
          className={mergeClasses(styles.dropdown, styles.fill)}
          size="small"
          aria-label={t("stereoTools.mode")}
          value={modeLabel(state.mode)}
          selectedOptions={[state.mode]}
          onOptionSelect={(_, data) => setState({ ...state, mode: data.optionValue as StereoMode })}
        >
          {modes.map((mode) => (
            <Option key={mode} value={mode}>
              {modeLabel(mode)}
            </Option>
          ))}
        </Dropdown>
        <InfoTip info={t("stereoTools.info.mode")} label={t("stereoTools.mode")} />
      </div>
      <ParameterSlider
        label={t("stereoTools.width")}
        info={t("stereoTools.info.width")}
        value={state.width}
        min={0}
        max={2}
        step={0.01}
        format={percent}
        onChange={(width) => setState({ ...state, width })}
      />
      <ParameterSlider
        label={t("stereoTools.balance")}
        info={t("stereoTools.info.balance")}
        value={state.balance}
        min={-1}
        max={1}
        step={0.01}
        format={balanceLabel}
        onChange={(balance) => setState({ ...state, balance })}
      />
      <div className={styles.withInfo}>
        <Switch
          label={t("stereoTools.invertLeft")}
          checked={state.invertLeft}
          onChange={(_, data) => setState({ ...state, invertLeft: data.checked })}
        />
        <InfoTip info={t("stereoTools.info.invertLeft")} label={t("stereoTools.invertLeft")} />
      </div>
      <div className={styles.withInfo}>
        <Switch
          label={t("stereoTools.invertRight")}
          checked={state.invertRight}
          onChange={(_, data) => setState({ ...state, invertRight: data.checked })}
        />
        <InfoTip info={t("stereoTools.info.invertRight")} label={t("stereoTools.invertRight")} />
      </div>
    </div>
  );
}
