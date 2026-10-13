import { decibels, milliseconds, ParameterSlider, useDefaultStyles, type WidgetProps } from "@micser/web-sdk";
import { useTranslation } from "../i18n";

export function DuckerWidget({ module, setState }: WidgetProps<"Ducker">) {
  const styles = useDefaultStyles();
  const { t } = useTranslation();
  const state = module.state;

  return (
    <div className={styles.column}>
      <ParameterSlider
        label={t("ducker.threshold")}
        value={state.threshold}
        min={-80}
        max={0}
        step={0.5}
        format={decibels}
        onChange={(threshold) => setState({ ...state, threshold })}
      />
      <ParameterSlider
        label={t("ducker.depth")}
        value={state.depth}
        min={-60}
        max={0}
        step={0.5}
        format={decibels}
        onChange={(depth) => setState({ ...state, depth })}
      />
      <ParameterSlider
        label={t("ducker.attack")}
        value={state.attack}
        min={0.0001}
        max={1}
        logarithmic
        format={milliseconds}
        onChange={(attack) => setState({ ...state, attack })}
      />
      <ParameterSlider
        label={t("ducker.hold")}
        value={state.hold}
        min={0}
        max={2}
        step={0.01}
        format={milliseconds}
        onChange={(hold) => setState({ ...state, hold })}
      />
      <ParameterSlider
        label={t("ducker.release")}
        value={state.release}
        min={0.001}
        max={5}
        logarithmic
        format={milliseconds}
        onChange={(release) => setState({ ...state, release })}
      />
    </div>
  );
}
