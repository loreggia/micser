import { Button, Dropdown, Option, makeStyles, tokens } from "@fluentui/react-components";
import { AddRegular, DeleteRegular } from "@fluentui/react-icons";
import { ParameterSlider, type EqualizerBand, type WidgetProps } from "@micser/web-sdk";

const useStyles = makeStyles({
  dropdown: {
    minWidth: 0,
  },
  column: {
    display: "flex",
    flexDirection: "column",
    gap: tokens.spacingVerticalS,
    width: "220px",
  },
  band: {
    display: "flex",
    flexDirection: "column",
    gap: tokens.spacingVerticalXS,
    paddingBottom: tokens.spacingVerticalS,
    borderBottom: `${tokens.strokeWidthThin} solid ${tokens.colorNeutralStroke2}`,
  },
  bandHeader: {
    display: "flex",
    justifyContent: "space-between",
    alignItems: "center",
    color: tokens.colorNeutralForeground2,
  },
});

const decibels = (value: number) => `${value > 0 ? "+" : ""}${value.toFixed(1)} dB`;
const milliseconds = (seconds: number) => `${Number((seconds * 1000).toPrecision(3))} ms`;
const hertz = (value: number) =>
  value >= 1000 ? `${Number((value / 1000).toPrecision(3))} kHz` : `${Math.round(value)} Hz`;

export function GainWidget({ module, setState }: WidgetProps<"Gain">) {
  const styles = useStyles();
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

export function CompressorWidget({ module, setState }: WidgetProps<"Compressor">) {
  const styles = useStyles();
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

const maxBands = 32;

export function EqualizerWidget({ module, setState }: WidgetProps<"Equalizer">) {
  const styles = useStyles();
  const bands = module.state.bands;
  const setBand = (index: number, band: EqualizerBand) => setState({ bands: bands.with(index, band) });

  return (
    <div className={styles.column}>
      {bands.map((band, index) => (
        <div key={index} className={styles.band}>
          <div className={styles.bandHeader}>
            <span>Band {index + 1}</span>
            <Button
              size="small"
              appearance="subtle"
              icon={<DeleteRegular />}
              aria-label={`Remove band ${index + 1}`}
              onClick={() => setState({ bands: bands.toSpliced(index, 1) })}
            />
          </div>
          <ParameterSlider
            label="Frequency"
            value={band.frequency}
            min={20}
            max={20000}
            logarithmic
            format={hertz}
            onChange={(frequency) => setBand(index, { ...band, frequency })}
          />
          <ParameterSlider
            label="Gain"
            value={band.gain}
            min={-24}
            max={24}
            step={0.5}
            format={decibels}
            onChange={(gain) => setBand(index, { ...band, gain })}
          />
          <ParameterSlider
            label="Q"
            value={band.q}
            min={0.1}
            max={20}
            logarithmic
            format={(v) => v.toFixed(2)}
            onChange={(q) => setBand(index, { ...band, q })}
          />
        </div>
      ))}
      <Button
        size="small"
        icon={<AddRegular />}
        disabled={bands.length >= maxBands}
        onClick={() => setState({ bands: [...bands, { frequency: 1000, gain: 0, q: 1.41 }] })}
      >
        Add band
      </Button>
    </div>
  );
}

export function PitchWidget({ module, setState }: WidgetProps<"Pitch">) {
  const styles = useStyles();
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
