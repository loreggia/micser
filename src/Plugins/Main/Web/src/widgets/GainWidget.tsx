import { decibels, ParameterSlider, useDefaultStyles, type WidgetProps } from "@micser/web-sdk";
import { useTranslation } from "../i18n";

export function GainWidget({ module, setState }: WidgetProps<"Gain">) {
  const styles = useDefaultStyles();
  const { t } = useTranslation();
  return (
    <div className={styles.column}>
      <ParameterSlider
        label={t("gain.gain")}
        value={module.state.gain}
        min={-60}
        max={24}
        step={0.5}
        format={decibels}
        onChange={(gain) => setState({ gain })}
      />
    </div>
  );
}
