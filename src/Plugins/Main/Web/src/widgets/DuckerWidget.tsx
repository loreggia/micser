import {
  decibels,
  milliseconds,
  ParameterKnob,
  parseMilliseconds,
  useDefaultStyles,
  type WidgetProps,
} from "@micser/web-sdk";
import { useTranslation } from "../i18n";

export function DuckerWidget({ module, setState, defaultState }: WidgetProps<"Ducker">) {
  const styles = useDefaultStyles();
  const { t } = useTranslation();
  const state = module.state;

  return (
    <div className={styles.column}>
      <div className={styles.knobs}>
        <ParameterKnob
          label={t("ducker.threshold")}
          info={t("ducker.info.threshold")}
          value={state.threshold}
          min={-80}
          max={0}
          step={0.5}
          format={decibels}
          defaultValue={defaultState?.threshold}
          onChange={(threshold) => setState({ ...state, threshold })}
        />
        <ParameterKnob
          label={t("ducker.depth")}
          info={t("ducker.info.depth")}
          value={state.depth}
          min={-60}
          max={0}
          step={0.5}
          format={decibels}
          defaultValue={defaultState?.depth}
          onChange={(depth) => setState({ ...state, depth })}
        />
        <ParameterKnob
          label={t("ducker.attack")}
          info={t("ducker.info.attack")}
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
          label={t("ducker.hold")}
          info={t("ducker.info.hold")}
          value={state.hold}
          min={0}
          max={2}
          step={0.01}
          format={milliseconds}
          parse={parseMilliseconds}
          defaultValue={defaultState?.hold}
          onChange={(hold) => setState({ ...state, hold })}
        />
        <ParameterKnob
          label={t("ducker.release")}
          info={t("ducker.info.release")}
          value={state.release}
          min={0.001}
          max={5}
          logarithmic
          format={milliseconds}
          parse={parseMilliseconds}
          defaultValue={defaultState?.release}
          onChange={(release) => setState({ ...state, release })}
        />
      </div>
    </div>
  );
}
