import { Caption1, makeStyles, tokens } from "@fluentui/react-components";
import {
  decibels,
  milliseconds,
  ParameterKnob,
  parseMilliseconds,
  useDefaultStyles,
  type WidgetProps,
} from "@micser/web-sdk";
import { useTranslation } from "../i18n";

/** The engine's LimiterModule.Lookahead in seconds: the latency the limiter adds. */
const lookahead = 0.005;

const useStyles = makeStyles({
  latency: {
    color: tokens.colorNeutralForeground3,
  },
});

export function LimiterWidget({ module, setState, defaultState }: WidgetProps<"Limiter">) {
  const defaultStyles = useDefaultStyles();
  const styles = useStyles();
  const { t } = useTranslation();
  const state = module.state;

  return (
    <div className={defaultStyles.column}>
      <div className={defaultStyles.knobs}>
        <ParameterKnob
          label={t("limiter.ceiling")}
          info={t("limiter.info.ceiling")}
          value={state.ceiling}
          min={-24}
          max={0}
          step={0.1}
          format={decibels}
          defaultValue={defaultState?.ceiling}
          onChange={(ceiling) => setState({ ...state, ceiling })}
        />
        <ParameterKnob
          label={t("limiter.gain")}
          info={t("limiter.info.gain")}
          value={state.gain}
          min={0}
          max={24}
          step={0.5}
          format={decibels}
          defaultValue={defaultState?.gain}
          onChange={(gain) => setState({ ...state, gain })}
        />
        <ParameterKnob
          label={t("limiter.release")}
          info={t("limiter.info.release")}
          value={state.release}
          min={0.001}
          max={2}
          logarithmic
          format={milliseconds}
          parse={parseMilliseconds}
          defaultValue={defaultState?.release}
          onChange={(release) => setState({ ...state, release })}
        />
      </div>
      <Caption1 className={styles.latency}>{t("limiter.latency", { value: milliseconds(lookahead) })}</Caption1>
    </div>
  );
}
