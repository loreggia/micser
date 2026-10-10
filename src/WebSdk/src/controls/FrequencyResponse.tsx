import { makeStyles, tokens } from "@fluentui/react-components";
import { useRef } from "react";
import { useLanguage } from "../i18n/i18n";
import { formatNumber, hertz } from "../lib/labels";
import { useElementSize } from "../lib/useElementSize";

const useStyles = makeStyles({
  // the graph doesn't widen its container: it is as wide as the other content
  root: {
    position: "relative",
  },
  svg: {
    position: "absolute",
    inset: 0,
    display: "block",
    width: "100%",
    height: "100%",
    borderRadius: tokens.borderRadiusMedium,
    backgroundColor: tokens.colorNeutralBackground3,
  },
  grid: {
    stroke: tokens.colorNeutralStroke2,
    strokeWidth: 1,
  },
  zero: {
    stroke: tokens.colorNeutralStroke1,
    strokeWidth: 1,
  },
  label: {
    fill: tokens.colorNeutralForeground3,
    fontSize: "9px",
    fontVariantNumeric: "tabular-nums",
  },
  curve: {
    fill: "none",
    stroke: tokens.colorBrandForeground1,
    strokeWidth: 1.5,
    strokeLinejoin: "round",
  },
  marker: {
    fill: tokens.colorBrandForeground1,
    stroke: tokens.colorNeutralBackground3,
    strokeWidth: 1,
  },
});

const minFrequency = 20;
const maxFrequency = 20000;
const gridFrequencies = [100, 1000, 10000];
const points = 200;

export interface FrequencyResponseProps {
  /**
   * The gain in dB at a frequency in Hz.
   */
  response: (frequency: number) => number;

  /**
   * Bottom and top of the dB axis.
   */
  minDecibels: number;
  maxDecibels: number;

  /**
   * Distance between the dB grid lines; default: 12.
   */
  decibelStep?: number;

  /**
   * Frequencies marked with a dot on the curve, e.g. an equalizer's bands.
   */
  markers?: readonly number[];

  /**
   * Accessible name of the graph.
   */
  label: string;
  height?: number;
}

/**
 * A frequency response curve on a logarithmic frequency axis (20 Hz to 20 kHz) and a linear dB axis. Values outside the dB range are
 * drawn at its edge. It fills the width of its container.
 */
export function FrequencyResponse({
  response,
  minDecibels,
  maxDecibels,
  decibelStep = 12,
  markers = [],
  label,
  height = 100,
}: FrequencyResponseProps) {
  const styles = useStyles();
  const rootRef = useRef<HTMLDivElement>(null);
  const { width } = useElementSize(rootRef, { width: 220, height });
  // labels change with the language
  useLanguage();

  const logRange = Math.log(maxFrequency / minFrequency);
  const x = (frequency: number) => (Math.log(frequency / minFrequency) / logRange) * width;
  const y = (decibels: number) => {
    const clamped = Math.min(maxDecibels, Math.max(minDecibels, decibels));
    return ((maxDecibels - clamped) / (maxDecibels - minDecibels)) * height;
  };

  let path = "";
  for (let i = 0; i <= points; i++) {
    const frequency = minFrequency * Math.pow(maxFrequency / minFrequency, i / points);
    path += `${i === 0 ? "M" : "L"}${x(frequency).toFixed(1)},${y(response(frequency)).toFixed(1)}`;
  }

  const gridDecibels: number[] = [];
  for (let d = Math.ceil(minDecibels / decibelStep) * decibelStep; d <= maxDecibels; d += decibelStep) {
    if (d > minDecibels && d < maxDecibels) {
      gridDecibels.push(d);
    }
  }

  return (
    <div ref={rootRef} className={styles.root} style={{ height }}>
      <svg className={styles.svg} viewBox={`0 0 ${width} ${height}`} role="img" aria-label={label}>
        {gridFrequencies.map((frequency) => (
          <g key={frequency}>
            <line className={styles.grid} x1={x(frequency)} x2={x(frequency)} y1={0} y2={height} />
            <text className={styles.label} x={x(frequency) - 2} y={height - 3} textAnchor="end">
              {hertz(frequency)}
            </text>
          </g>
        ))}
        {gridDecibels.map((decibels) => (
          <g key={decibels}>
            <line
              className={decibels === 0 ? styles.zero : styles.grid}
              x1={0}
              x2={width}
              y1={y(decibels)}
              y2={y(decibels)}
            />
            <text className={styles.label} x={2} y={y(decibels) - 2}>
              {formatNumber(decibels, { signDisplay: "exceptZero" })}
            </text>
          </g>
        ))}
        <path className={styles.curve} d={path} />
        {markers.map((frequency, index) => (
          <circle key={index} className={styles.marker} cx={x(frequency)} cy={y(response(frequency))} r={3} />
        ))}
      </svg>
    </div>
  );
}
