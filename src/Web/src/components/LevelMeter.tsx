import { Caption2, makeStyles, mergeClasses, tokens } from "@fluentui/react-components";
import { useModuleLevels, type PortLevels } from "@micser/web-sdk";
import { useState } from "react";

/** The lowest level shown, in dBFS. */
const floorDb = -60;

/** Level updates (about 20 per second) a peak stays marked before it falls back. */
const holdUpdates = 30;

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
  rms: {
    position: "absolute",
    top: 0,
    bottom: 0,
    left: 0,
    backgroundColor: tokens.colorBrandBackground,
    transition: "width 50ms linear",
  },
  peak: {
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
  holds: Hold[][];
}

/**
 * Peak and RMS level of each channel of a module's outputs, after volume and mute. The peak marker is held for about 1.5 s and turns red
 * at full scale.
 */
export function LevelMeter({ moduleId }: { moduleId: string }) {
  const styles = useStyles();
  const levels = useModuleLevels(moduleId);
  const [state, setState] = useState<MeterState>({ shown: [], holds: [] });

  // each update is a new object, so this advances the peak holds once per update
  if (levels !== state.levels) {
    const shown =
      levels ?? state.shown.map((port) => ({ ...port, peak: port.peak.map(() => 0), rms: port.rms.map(() => 0) }));
    const holds = shown.map((port, p) =>
      port.peak.map((peak, c): Hold => {
        const hold = state.holds[p]?.[c];
        return !levels || !hold || peak >= hold.value || hold.age >= holdUpdates
          ? { value: peak, age: 0 }
          : { value: hold.value, age: hold.age + 1 };
      })
    );
    setState({ levels, shown, holds });
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
                  <div className={styles.rms} style={{ width: `${toPosition(rms) * 100}%` }} />
                  {hold > 0 && (
                    <div
                      className={mergeClasses(styles.peak, hold >= 1 && styles.clipped)}
                      style={{ left: `calc(${toPosition(hold) * 100}% - 2px)` }}
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

/** Maps a linear level to 0..1 on the meter's dB scale. */
function toPosition(linear: number) {
  return Math.min((toDecibels(linear) - floorDb) / -floorDb, 1);
}
