import { Caption1, makeStyles, tokens } from "@fluentui/react-components";
import { decibels, milliseconds, ParameterSlider, useDefaultStyles, type WidgetProps } from "@micser/web-sdk";
import { useTranslation } from "../i18n";

/** The engine's LimiterModule.Lookahead in seconds: the latency the limiter adds. */
const lookahead = 0.005;

const useStyles = makeStyles({
  latency: {
    color: tokens.colorNeutralForeground3,
  },
});

export function LimiterWidget({ module, setState }: WidgetProps<"Limiter">) {
  const defaultStyles = useDefaultStyles();
  const styles = useStyles();
  const { t } = useTranslation();
  const state = module.state;

  return (
    <div className={defaultStyles.column}>
      <ParameterSlider
        label={t("limiter.ceiling")}
        value={state.ceiling}
        min={-24}
        max={0}
        step={0.1}
        format={decibels}
        onChange={(ceiling) => setState({ ...state, ceiling })}
      />
      <ParameterSlider
        label={t("limiter.gain")}
        value={state.gain}
        min={0}
        max={24}
        step={0.5}
        format={decibels}
        onChange={(gain) => setState({ ...state, gain })}
      />
      <ParameterSlider
        label={t("limiter.release")}
        value={state.release}
        min={0.001}
        max={2}
        logarithmic
        format={milliseconds}
        onChange={(release) => setState({ ...state, release })}
      />
      <Caption1 className={styles.latency}>{t("limiter.latency", { value: milliseconds(lookahead) })}</Caption1>
    </div>
  );
}
