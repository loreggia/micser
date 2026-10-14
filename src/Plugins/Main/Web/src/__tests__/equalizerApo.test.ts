import { expect, test } from "vitest";
import { maxBands, parseEqualizerApo } from "../equalizerApo";

test("reads an AutoEQ parametric EQ", () => {
  const result = parseEqualizerApo(
    [
      "Preamp: -6.2 dB",
      "Filter 1: ON LSC Fc 105 Hz Gain 5.8 dB Q 0.70",
      "Filter 2: ON PK Fc 179 Hz Gain -3.3 dB Q 0.39",
      "Filter 3: ON HSC Fc 10000 Hz Gain 2.1 dB Q 0.70",
    ].join("\r\n")
  );

  expect(result).toEqual({
    preamp: -6.2,
    skipped: 0,
    bands: [
      { type: "LowShelf", frequency: 105, gain: 5.8, q: 0.7 },
      { type: "Peaking", frequency: 179, gain: -3.3, q: 0.39 },
      { type: "HighShelf", frequency: 10000, gain: 2.1, q: 0.7 },
    ],
  });
});

test("reads pass filters, bandwidths and missing Qs, and skips filters that are off", () => {
  const result = parseEqualizerApo(
    [
      "# headphones",
      "Filter: ON HP Fc 30 Hz",
      "Filter: ON LPQ Fc 18000 Hz Q 1",
      "Filter: ON PK Fc 1000 Hz Gain 2 dB BW Oct 1",
      "Filter: OFF PK Fc 2000 Hz Gain 2 dB Q 1",
    ].join("\n")
  );

  expect(result?.preamp).toBe(0);
  expect(result?.skipped).toBe(0);
  expect(result?.bands).toEqual([
    { type: "HighPass", frequency: 30, gain: 0, q: Math.SQRT1_2 },
    { type: "LowPass", frequency: 18000, gain: 0, q: 1 },
    { type: "Peaking", frequency: 1000, gain: 2, q: expect.closeTo(1.414, 3) as number },
  ]);
});

test("skips unsupported types and filters for single channels", () => {
  const result = parseEqualizerApo(
    [
      "Filter: ON NO Fc 50 Hz",
      "Channel: L",
      "Preamp: -3 dB",
      "Filter: ON PK Fc 100 Hz Gain 3 dB Q 1",
      "Channel: all",
      "Filter: ON PK Fc 200 Hz Gain 3 dB Q 1",
    ].join("\n")
  );

  expect(result?.preamp).toBe(0);
  expect(result?.skipped).toBe(2);
  expect(result?.bands.map((band) => band.frequency)).toEqual([200]);
});

test("limits values to the equalizer's ranges and its band count", () => {
  const lines = Array.from({ length: maxBands + 2 }, () => "Filter: ON PK Fc 10 Hz Gain 30 dB Q 50");
  const result = parseEqualizerApo(["Preamp: -40 dB", ...lines].join("\n"));

  expect(result?.preamp).toBe(-24);
  expect(result?.bands).toHaveLength(maxBands);
  expect(result?.bands[0]).toEqual({ type: "Peaking", frequency: 20, gain: 24, q: 20 });
  expect(result?.skipped).toBe(2);
});

test("returns undefined without filters or preamp", () => {
  expect(parseEqualizerApo("Device: Speakers\nInclude: other.txt")).toBeUndefined();
});
