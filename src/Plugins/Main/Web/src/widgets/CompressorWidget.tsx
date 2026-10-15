import { Dropdown, mergeClasses, Option } from "@fluentui/react-components";
import {
  decibels,
  formatNumber,
  InfoTip,
  milliseconds,
  parsePercent,
  ParameterKnob,
  parseMilliseconds,
  useDefaultStyles,
  type WidgetProps,
} from "@micser/web-sdk";
import { useTranslation } from "../i18n";

export function CompressorWidget({ module, setState, defaultState }: WidgetProps<"Compressor">) {
  const styles = useDefaultStyles();
  const { t } = useTranslation();
  const state = module.state;

  return (
    <div className={styles.column}>
      <div className={styles.withInfo}>
        <Dropdown
          className={mergeClasses(styles.dropdown, styles.fill)}
          size="small"
          aria-label={t("compressor.type")}
          value={state.type === "Downward" ? t("compressor.downward") : t("compressor.upward")}
          selectedOptions={[state.type]}
          onOptionSelect={(_, data) => setState({ ...state, type: data.optionValue as typeof state.type })}
        >
          <Option value="Downward">{t("compressor.downward")}</Option>
          <Option value="Upward">{t("compressor.upward")}</Option>
        </Dropdown>
        <InfoTip info={t("compressor.info.type")} label={t("compressor.type")} />
      </div>
      <div className={styles.knobs}>
        <ParameterKnob
          label={t("compressor.threshold")}
          info={t("compressor.info.threshold")}
          value={state.threshold}
          min={-80}
          max={0}
          step={0.5}
          format={decibels}
          defaultValue={defaultState?.threshold}
          onChange={(threshold) => setState({ ...state, threshold })}
        />
        <ParameterKnob
          label={t("compressor.ratio")}
          info={t("compressor.info.ratio")}
          value={state.ratio}
          min={1}
          max={20}
          logarithmic
          format={(v) => `${formatNumber(v, { minimumFractionDigits: 1, maximumFractionDigits: 1 })}:1`}
          defaultValue={defaultState?.ratio}
          onChange={(ratio) => setState({ ...state, ratio })}
        />
        <ParameterKnob
          label={t("compressor.attack")}
          info={t("compressor.info.attack")}
          value={state.attack}
          min={0.0001}
          max={1}
          logarithmic
          format={milliseconds}
          parse={parseMilliseconds}
          defaultValue={defaultState?.attack}
          onChange={(attack) => setState({ ...state, attack })}
        />
        <ParameterKnob
          label={t("compressor.release")}
          info={t("compressor.info.release")}
          value={state.release}
          min={0.001}
          max={5}
          logarithmic
          format={milliseconds}
          parse={parseMilliseconds}
          defaultValue={defaultState?.release}
          onChange={(release) => setState({ ...state, release })}
        />
        <ParameterKnob
          label={t("compressor.knee")}
          info={t("compressor.info.knee")}
          value={state.knee}
          min={0}
          max={24}
          step={0.5}
          unit="dB"
          defaultValue={defaultState?.knee}
          onChange={(knee) => setState({ ...state, knee })}
        />
        <ParameterKnob
          label={t("compressor.makeUpGain")}
          info={t("compressor.info.makeUpGain")}
          value={state.makeUpGain}
          min={-24}
          max={24}
          step={0.5}
          format={decibels}
          defaultValue={defaultState?.makeUpGain}
          onChange={(makeUpGain) => setState({ ...state, makeUpGain })}
        />
        <ParameterKnob
          label={t("compressor.amount")}
          info={t("compressor.info.amount")}
          value={state.amount}
          min={0}
          max={1}
          step={0.01}
          format={(v) => formatNumber(v, { style: "percent", maximumFractionDigits: 0 })}
          parse={parsePercent}
          defaultValue={defaultState?.amount}
          onChange={(amount) => setState({ ...state, amount })}
        />
      </div>
    </div>
  );
}
