import { Label, Slider, makeStyles, tokens, useId } from "@fluentui/react-components";
import { useLanguage } from "../i18n/i18n";
import { formatNumber } from "../lib/labels";

const useStyles = makeStyles({
  root: {
    display: "grid",
    gridTemplateColumns: "1fr auto",
    alignItems: "center",
    columnGap: tokens.spacingHorizontalS,
  },
  value: {
    color: tokens.colorNeutralForeground2,
    fontVariantNumeric: "tabular-nums",
  },
  slider: {
    gridColumn: "1 / span 2",
    minWidth: 0,
  },
  rail: {
    // no tick mark per step
    "::before": {
      backgroundImage: "none",
    },
  },
});

/** Positions of a logarithmic slider, and of a linear one without a step. */
const defaultPositions = 1000;

export interface ParameterSliderProps {
  label: string;
  value: number;
  min: number;
  max: number;
  /**
   * Distance between values of a linear slider, also the keyboard step; default: 1/100 of the range.
   */
  step?: number;

  /**
   * Logarithmic scale, e.g. for frequencies. Needs min > 0.
   */
  logarithmic?: boolean;

  /**
   * Formats the value shown next to the label; default: the value with its unit.
   */
  format?: (value: number) => string;
  unit?: string;
  onChange: (value: number) => void;
}

/**
 * A labeled slider for a module parameter. Inside graph nodes, wrap controls in an element with the `nodrag` class.
 */
export function ParameterSlider({
  label,
  value,
  min,
  max,
  step,
  logarithmic,
  format,
  unit,
  onChange,
}: ParameterSliderProps) {
  const styles = useStyles();
  const id = useId("parameter");
  // formats change with the language
  useLanguage();
  const text = format ? format(value) : `${formatNumber(value, { maximumFractionDigits: 2 })}${unit ? ` ${unit}` : ""}`;

  // The slider works in integer positions: fractional steps like 1/12 aren't exact as floats, and the native range
  // input would snap values to slightly different ones.
  const positions = logarithmic ? defaultPositions : Math.round((max - min) / (step ?? (max - min) / 100));
  const toPosition = (v: number) =>
    Math.round(
      logarithmic ? (Math.log(v / min) / Math.log(max / min)) * positions : ((v - min) / (max - min)) * positions
    );
  const fromPosition = (p: number) =>
    logarithmic ? min * Math.pow(max / min, p / positions) : min + (p / positions) * (max - min);

  return (
    <div className={styles.root}>
      <Label htmlFor={id} size="small">
        {label}
      </Label>
      <span className={styles.value}>{text}</span>
      <Slider
        id={id}
        className={styles.slider}
        size="small"
        min={0}
        max={positions}
        step={1}
        rail={{ className: styles.rail }}
        value={toPosition(value)}
        onChange={(_, data) => onChange(fromPosition(data.value))}
      />
    </div>
  );
}
