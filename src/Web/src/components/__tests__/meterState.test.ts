import type { PortLevels } from "@micser/web-sdk";
import { describe, expect, test } from "vitest";
import {
  advanceMeter,
  floorDb,
  holdUpdates,
  initialMeterState,
  peakFallDb,
  toDecibels,
  toPosition,
  type MeterState,
} from "../meterState";

function levels(peak: number, rms = peak / 2): PortLevels[] {
  return [{ port: "Output", peak: [peak], rms: [rms] }];
}

function advance(state: MeterState, ...updates: (PortLevels[] | undefined)[]) {
  return updates.reduce(advanceMeter, state);
}

describe("scale", () => {
  test("converts to dBFS down to the floor", () => {
    expect(toDecibels(1)).toBe(0);
    expect(toDecibels(0.5)).toBeCloseTo(-6.02);
    expect(toDecibels(1e-6)).toBe(floorDb);
    expect(toDecibels(0)).toBe(floorDb);
  });

  test("maps the floor to 0 and full scale and above to 1", () => {
    expect(toPosition(floorDb)).toBe(0);
    expect(toPosition(-30)).toBe(0.5);
    expect(toPosition(0)).toBe(1);
    expect(toPosition(6)).toBe(1);
  });
});

describe("advanceMeter", () => {
  test("shows the first levels as they are", () => {
    const state = advanceMeter(initialMeterState, levels(0.5));

    expect(state.shown).toEqual(levels(0.5));
    expect(state.peaks[0][0]).toBeCloseTo(-6.02);
    expect(state.holds).toEqual([[{ value: 0.5, age: 0 }]]);
  });

  test("the peak bar rises at once and falls slowly", () => {
    const loud = advanceMeter(initialMeterState, levels(1));
    const quieter = advance(loud, levels(0.01), levels(0.01));

    expect(loud.peaks[0][0]).toBe(0);
    expect(quieter.peaks[0][0]).toBe(-2 * peakFallDb);
    expect(advanceMeter(quieter, levels(1)).peaks[0][0]).toBe(0);
  });

  test("the peak bar doesn't fall below the current peak", () => {
    const state = advance(initialMeterState, levels(1), levels(0.5));

    expect(state.peaks[0][0]).toBe(-peakFallDb);
    expect(advance(state, ...Array.from({ length: 10 }, () => levels(0.5))).peaks[0][0]).toBeCloseTo(-6.02);
  });

  test("holds the highest peak, then lets it go", () => {
    let state = advanceMeter(initialMeterState, levels(0.8));
    for (let i = 0; i < holdUpdates; i++) {
      state = advanceMeter(state, levels(0.1));
    }

    expect(state.holds[0][0]).toEqual({ value: 0.8, age: holdUpdates });
    expect(advanceMeter(state, levels(0.1)).holds[0][0]).toEqual({ value: 0.1, age: 0 });
  });

  test("a higher peak replaces the hold", () => {
    const state = advance(initialMeterState, levels(0.5), levels(0.1), levels(0.9));

    expect(state.holds[0][0]).toEqual({ value: 0.9, age: 0 });
  });

  test("keeps the shape at zero while the module isn't processed", () => {
    const stereo: PortLevels[] = [{ port: null, peak: [0.5, 0.4], rms: [0.2, 0.1] }];
    const state = advance(initialMeterState, stereo, undefined);

    expect(state.levels).toBeUndefined();
    expect(state.shown).toEqual([{ port: null, peak: [0, 0], rms: [0, 0] }]);
    expect(state.peaks).toEqual([[floorDb, floorDb]]);
    expect(state.holds).toEqual([
      [
        { value: 0, age: 0 },
        { value: 0, age: 0 },
      ],
    ]);
  });
});
