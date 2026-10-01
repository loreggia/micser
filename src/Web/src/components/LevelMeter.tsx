import { Caption2, makeStyles, mergeClasses, tokens } from "@fluentui/react-components";
import { useModuleLevels, type PortLevels } from "@micser/web-sdk";
import { useState } from "react";

/** The lowest level shown, in dBFS. */
const floorDb = -60;

/** Level updates (about 20 per second) a peak stays marked before it falls back. */
const holdUpdates = 30;

/** How far the peak bar falls per level update: 20 dB/s. */
const peakFallDb = 1;

const useStyles = makeStyles({
  root: {
    display: "flex",
    flexDirection: "column",
    gap: tokens.spacingVerticalXXS,
  },
  port: {
    display: "grid",
    gridTemplateColumns: "3em 1fr",
    alignItems: "center",
  },
  portLabel: {
    color: tokens.colorNeutralForeground3,
  },
  channels: {
    display: "flex",
    flexDirection: "column",
    gap: "2px",
  },
  bar: {
    position: "relative",
    height: "4px",
    overflow: "hidden",
    borderRadius: tokens.borderRadiusSmall,
    backgroundColor: tokens.colorNeutralBackground5,
  },
  level: {
    position: "absolute",
    top: 0,
    bottom: 0,
    left: 0,
    backgroundColor: tokens.colorBrandBackground,
    transition: "width 50ms linear",
  },
  peakLevel: {
    opacity: 0.55,
  },
  hold: {
    position: "absolute",
    top: 0,
    bottom: 0,
    width: "2px",
    backgroundColor: tokens.colorNeutralForeground2,
  },
  clipped: {
    backgroundColor: tokens.colorPaletteRedBackground3,
  },
});

interface Hold {
  value: number;
  age: number;
}

interface MeterState {
  levels?: PortLevels[];
  /** The last levels, or zeros of their shape while the module isn't processed, so the meter keeps its height. */
  shown: PortLevels[];
  /** The peak bars in dB: they rise with the peak and fall at {@link peakFallDb} per update. */
  peaks: number[][];
  holds: Hold[][];
}

/**
 * Studio-style meter of each channel of a module's outputs, after volume and mute: the RMS level as a solid bar, the peak level as a
 * lighter bar behind it that falls back slowly, and the highest peak as a marker held for about 1.5 s that turns red at full scale.
 */
export function LevelMeter({ moduleId }: { moduleId: string }) {
  const styles = useStyles();
  const levels = useModuleLevels(moduleId);
  const [state, setState] = useState<MeterState>({ shown: [], peaks: [], holds: [] });

  // each update is a new object, so this advances the peak bars and holds once per update
  if (levels !== state.levels) {
    const shown =
      levels ?? state.shown.map((port) => ({ ...port, peak: port.peak.map(() => 0), rms: port.rms.map(() => 0) }));
    const peaks = shown.map((port, p) =>
      port.peak.map((peak, c) =>
        levels ? Math.max(toDecibels(peak), (state.peaks[p]?.[c] ?? floorDb) - peakFallDb) : floorDb
      )
    );
    const holds = shown.map((port, p) =>
      port.peak.map((peak, c): Hold => {
        const hold = state.holds[p]?.[c];
        return !levels || !hold || peak >= hold.value || hold.age >= holdUpdates
          ? { value: peak, age: 0 }
          : { value: hold.value, age: hold.age + 1 };
      })
    );
    setState({ levels, shown, peaks, holds });
  }

  if (state.shown.length === 0) {
    return null;
  }

  return (
    <div className={styles.root}>
      {state.shown.map((port, p) => (
        <div key={port.port ?? ""} className={state.shown.length > 1 ? styles.port : undefined}>
          {state.shown.length > 1 && <Caption2 className={styles.portLabel}>{port.port}</Caption2>}
          <div className={styles.channels}>
            {port.rms.map((rms, c) => {
              const hold = state.holds[p]?.[c]?.value ?? 0;
              return (
                <div
                  key={c}
                  className={styles.bar}
                  role="meter"
                  aria-label={`Channel ${c + 1} level`}
                  aria-valuemin={floorDb}
                  aria-valuemax={0}
                  aria-valuenow={Math.round(toDecibels(port.peak[c]))}
                >
                  <div
                    className={mergeClasses(styles.level, styles.peakLevel)}
                    style={{ width: `${toPosition(state.peaks[p]?.[c] ?? floorDb) * 100}%` }}
                  />
                  <div className={styles.level} style={{ width: `${toPosition(toDecibels(rms)) * 100}%` }} />
                  {hold > 0 && (
                    <div
                      className={mergeClasses(styles.hold, hold >= 1 && styles.clipped)}
                      style={{ left: `calc(${toPosition(toDecibels(hold)) * 100}% - 2px)` }}
                    />
                  )}
                </div>
              );
            })}
          </div>
        </div>
      ))}
    </div>
  );
}

function toDecibels(linear: number) {
  return linear > 0 ? Math.max(20 * Math.log10(linear), floorDb) : floorDb;
}

/** Maps a level in dB to 0..1 on the meter's scale. */
function toPosition(decibels: number) {
  return Math.min((decibels - floorDb) / -floorDb, 1);
}
