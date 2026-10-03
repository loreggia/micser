import { Dropdown, Option } from "@fluentui/react-components";
import { decibels, milliseconds, ParameterSlider, useDefaultStyles, type WidgetProps } from "@micser/web-sdk";

export function CompressorWidget({ module, setState }: WidgetProps<"Compressor">) {
  const styles = useDefaultStyles();
  const state = module.state;

  return (
    <div className={styles.column}>
      <Dropdown
        className={styles.dropdown}
        size="small"
        value={state.type === "Downward" ? "Downward" : "Upward"}
        selectedOptions={[state.type]}
        onOptionSelect={(_, data) => setState({ ...state, type: data.optionValue as typeof state.type })}
      >
        <Option value="Downward">Downward</Option>
        <Option value="Upward">Upward</Option>
      </Dropdown>
      <ParameterSlider
        label="Threshold"
        value={state.threshold}
        min={-80}
        max={0}
        step={0.5}
        format={decibels}
        onChange={(threshold) => setState({ ...state, threshold })}
      />
      <ParameterSlider
        label="Ratio"
        value={state.ratio}
        min={1}
        max={20}
        logarithmic
        format={(v) => `${v.toFixed(1)}:1`}
        onChange={(ratio) => setState({ ...state, ratio })}
      />
      <ParameterSlider
        label="Attack"
        value={state.attack}
        min={0.0001}
        max={1}
        logarithmic
        format={milliseconds}
        onChange={(attack) => setState({ ...state, attack })}
      />
      <ParameterSlider
        label="Release"
        value={state.release}
        min={0.001}
        max={5}
        logarithmic
        format={milliseconds}
        onChange={(release) => setState({ ...state, release })}
      />
      <ParameterSlider
        label="Knee"
        value={state.knee}
        min={0}
        max={24}
        step={0.5}
        unit="dB"
        onChange={(knee) => setState({ ...state, knee })}
      />
      <ParameterSlider
        label="Make-up gain"
        value={state.makeUpGain}
        min={-24}
        max={24}
        step={0.5}
        format={decibels}
        onChange={(makeUpGain) => setState({ ...state, makeUpGain })}
      />
      <ParameterSlider
        label="Amount"
        value={state.amount}
        min={0}
        max={1}
        step={0.01}
        format={(v) => `${Math.round(v * 100)}%`}
        onChange={(amount) => setState({ ...state, amount })}
      />
    </div>
  );
}
