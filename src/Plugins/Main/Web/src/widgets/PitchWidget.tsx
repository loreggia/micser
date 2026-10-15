import { formatNumber, ParameterKnob, parseNumber, useDefaultStyles, type WidgetProps } from "@micser/web-sdk";
import { useTranslation } from "../i18n";

export function PitchWidget({ module, setState }: WidgetProps<"Pitch">) {
  const styles = useDefaultStyles();
  const { t } = useTranslation();
  const state = module.state;

  return (
    <div className={styles.column}>
      <div className={styles.knobs}>
        <ParameterKnob
          label={t("pitch.pitch")}
          info={t("pitch.info.pitch")}
          value={state.pitch}
          min={-1}
          max={1}
          step={1 / 12}
          format={(v) =>
            t("pitch.semitones", { value: formatNumber(Math.round(v * 12), { signDisplay: "exceptZero" }) })
          }
          parse={(text) => {
            const semitones = parseNumber(text);
            return semitones === undefined ? undefined : semitones / 12;
          }}
          onChange={(pitch) => setState({ ...state, pitch })}
        />
        <ParameterKnob
          label={t("pitch.quality")}
          info={t("pitch.info.quality")}
          value={state.quality}
          min={1}
          max={10}
          step={1}
          format={(v) => String(v)}
          onChange={(quality) => setState({ ...state, quality })}
        />
      </div>
    </div>
  );
}
