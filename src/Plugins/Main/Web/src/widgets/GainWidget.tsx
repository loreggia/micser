import { decibels, ParameterSlider, useDefaultStyles, type WidgetProps } from "@micser/web-sdk";

export function GainWidget({ module, setState }: WidgetProps<"Gain">) {
  const styles = useDefaultStyles();
  return (
    <div className={styles.column}>
      <ParameterSlider
        label="Gain"
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
