import type { PortLevels } from "@micser/web-sdk";

/** The lowest level shown, in dBFS. */
export const floorDb = -60;

/** Level updates (about 20 per second) a peak stays marked before it falls back. */
export const holdUpdates = 30;

/** How far the peak bar falls per level update: 20 dB/s. */
export const peakFallDb = 1;

export interface Hold {
  value: number;
  age: number;
}

export interface MeterState {
  levels?: PortLevels[];
  /** The last levels, or zeros of their shape while the module isn't processed, so the meter keeps its height. */
  shown: PortLevels[];
  /** The peak bars in dB: they rise with the peak and fall at {@link peakFallDb} per update. */
  peaks: number[][];
  holds: Hold[][];
}

export const initialMeterState: MeterState = { shown: [], peaks: [], holds: [] };

/**
 * The meter after a level update, or after the module stopped being processed (no levels).
 */
export function advanceMeter(state: MeterState, levels: PortLevels[] | undefined): MeterState {
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
  return { levels, shown, peaks, holds };
}

export function toDecibels(linear: number) {
  return linear > 0 ? Math.max(20 * Math.log10(linear), floorDb) : floorDb;
}

/** Maps a level in dB to 0..1 on the meter's scale. */
export function toPosition(decibels: number) {
  return Math.min((decibels - floorDb) / -floorDb, 1);
}
