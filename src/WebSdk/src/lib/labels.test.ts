import { expect, test } from "vitest";
import { decibels, hertz, milliseconds } from "./labels";

test("decibels have one decimal and a sign when positive", () => {
  expect(decibels(3)).toBe("+3.0 dB");
  expect(decibels(0)).toBe("0.0 dB");
  expect(decibels(-6.04)).toBe("-6.0 dB");
});

test("milliseconds have three significant digits", () => {
  expect(milliseconds(0.0123456)).toBe("12.3 ms");
  expect(milliseconds(0.0001)).toBe("0.1 ms");
  expect(milliseconds(1)).toBe("1000 ms");
});

test("hertz switch to kilohertz at 1000", () => {
  expect(hertz(440.4)).toBe("440 Hz");
  expect(hertz(1000)).toBe("1 kHz");
  expect(hertz(12345)).toBe("12.3 kHz");
});
