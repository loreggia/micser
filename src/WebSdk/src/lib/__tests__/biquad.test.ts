import { expect, test } from "vitest";
import { biquadResponse, highPass, lowPass, peakingEq } from "../biquad";

const sampleRate = 48000;

test("a peaking EQ has its gain at the center frequency and none far away", () => {
  const filter = [peakingEq(sampleRate, 1000, 1.41, 6)];

  expect(biquadResponse(filter, sampleRate, 1000)).toBeCloseTo(6, 3);
  expect(biquadResponse(filter, sampleRate, 20)).toBeCloseTo(0, 1);
});

test("biquads in series add their gains", () => {
  const filters = [peakingEq(sampleRate, 1000, 1, 6), peakingEq(sampleRate, 1000, 1, -2)];

  expect(biquadResponse(filters, sampleRate, 1000)).toBeCloseTo(4, 3);
});

test("a Butterworth low-pass is 3 dB down at the cutoff and falls by 12 dB per octave", () => {
  const filter = [lowPass(sampleRate, 1000, Math.SQRT1_2)];

  expect(biquadResponse(filter, sampleRate, 20)).toBeCloseTo(0, 2);
  expect(biquadResponse(filter, sampleRate, 1000)).toBeCloseTo(-3.01, 1);
  const octave = biquadResponse(filter, sampleRate, 4000) - biquadResponse(filter, sampleRate, 8000);
  expect(octave).toBeGreaterThan(11);
});

test("a high-pass passes high frequencies and has the Q as its gain at the cutoff", () => {
  const filter = [highPass(sampleRate, 100, 2)];

  expect(biquadResponse(filter, sampleRate, 10000)).toBeCloseTo(0, 2);
  expect(biquadResponse(filter, sampleRate, 100)).toBeCloseTo(20 * Math.log10(2), 1);
  expect(biquadResponse(filter, sampleRate, 25)).toBeLessThan(-20);
});
