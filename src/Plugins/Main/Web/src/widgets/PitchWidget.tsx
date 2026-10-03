import { ParameterSlider, useDefaultStyles, type WidgetProps } from "@micser/web-sdk";

export function PitchWidget({ module, setState }: WidgetProps<"Pitch">) {
  const styles = useDefaultStyles();
  const state = module.state;

  return (
    <div className={styles.column}>
      <ParameterSlider
        label="Pitch"
        value={state.pitch}
        min={-1}
        max={1}
        step={1 / 12}
        format={(v) => `${v > 0 ? "+" : ""}${Math.round(v * 12)} st`}
        onChange={(pitch) => setState({ ...state, pitch })}
      />
      <ParameterSlider
        label="Quality"
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
