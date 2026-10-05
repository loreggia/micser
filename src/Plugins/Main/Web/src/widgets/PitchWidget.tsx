import { formatNumber, ParameterSlider, useDefaultStyles, type WidgetProps } from "@micser/web-sdk";
import { useTranslation } from "../i18n";

export function PitchWidget({ module, setState }: WidgetProps<"Pitch">) {
  const styles = useDefaultStyles();
  const { t } = useTranslation();
  const state = module.state;

  return (
    <div className={styles.column}>
      <ParameterSlider
        label={t("pitch.pitch")}
        value={state.pitch}
        min={-1}
        max={1}
        step={1 / 12}
        format={(v) => t("pitch.semitones", { value: formatNumber(Math.round(v * 12), { signDisplay: "exceptZero" }) })}
        onChange={(pitch) => setState({ ...state, pitch })}
      />
      <ParameterSlider
        label={t("pitch.quality")}
        value={state.quality}
        min={1}
        max={10}
        step={1}
        format={(v) => String(v)}
        onChange={(quality) => setState({ ...state, quality })}
      />
    </div>
  );
}
