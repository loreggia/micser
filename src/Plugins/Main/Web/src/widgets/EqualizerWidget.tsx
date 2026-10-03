import { Button, makeStyles, tokens } from "@fluentui/react-components";
import { AddRegular, DeleteRegular } from "@fluentui/react-icons";
import {
  decibels,
  hertz,
  ParameterSlider,
  useDefaultStyles,
  type EqualizerBand,
  type WidgetProps,
} from "@micser/web-sdk";

const useStyles = makeStyles({
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

const maxBands = 32;

export function EqualizerWidget({ module, setState }: WidgetProps<"Equalizer">) {
  const defaultStyles = useDefaultStyles();
  const styles = useStyles();
  const bands = module.state.bands;
  const setBand = (index: number, band: EqualizerBand) => setState({ bands: bands.with(index, band) });

  return (
    <div className={defaultStyles.column}>
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
