import { decibels, milliseconds, ParameterSlider, useDefaultStyles, type WidgetProps } from "@micser/web-sdk";
import { useTranslation } from "../i18n";

export function NoiseGateWidget({ module, setState }: WidgetProps<"NoiseGate">) {
  const styles = useDefaultStyles();
  const { t } = useTranslation();
  const state = module.state;

  return (
    <div className={styles.column}>
      <ParameterSlider
        label={t("noiseGate.threshold")}
        info={t("noiseGate.info.threshold")}
        value={state.threshold}
        min={-100}
        max={0}
        step={0.5}
        format={decibels}
        onChange={(threshold) => setState({ ...state, threshold })}
      />
      <ParameterSlider
        label={t("noiseGate.range")}
        info={t("noiseGate.info.range")}
        value={state.range}
        min={-100}
        max={0}
        step={0.5}
        format={decibels}
        onChange={(range) => setState({ ...state, range })}
      />
      <ParameterSlider
        label={t("noiseGate.attack")}
        info={t("noiseGate.info.attack")}
        value={state.attack}
        min={0.0001}
        max={1}
        logarithmic
        format={milliseconds}
        onChange={(attack) => setState({ ...state, attack })}
      />
      <ParameterSlider
        label={t("noiseGate.hold")}
        info={t("noiseGate.info.hold")}
        value={state.hold}
        min={0}
        max={2}
        step={0.01}
        format={milliseconds}
        onChange={(hold) => setState({ ...state, hold })}
      />
      <ParameterSlider
        label={t("noiseGate.release")}
        info={t("noiseGate.info.release")}
        value={state.release}
        min={0.001}
        max={5}
        logarithmic
        format={milliseconds}
        onChange={(release) => setState({ ...state, release })}
      />
      <ParameterSlider
        label={t("noiseGate.hysteresis")}
        info={t("noiseGate.info.hysteresis")}
        value={state.hysteresis}
        min={0}
        max={20}
        step={0.5}
        unit="dB"
        onChange={(hysteresis) => setState({ ...state, hysteresis })}
      />
    </div>
  );
}
