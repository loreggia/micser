import { afterEach, expect, test } from "vitest";
import { i18n } from "../../i18n/i18n";
import { decibels, formatNumber, hertz, milliseconds } from "../labels";

afterEach(() => i18n.changeLanguage("en"));

test("decibels have one decimal and a sign when not zero", () => {
  expect(decibels(3)).toBe("+3.0 dB");
  expect(decibels(0)).toBe("0.0 dB");
  expect(decibels(-6.04)).toBe("-6.0 dB");
  expect(decibels(-0.01)).toBe("0.0 dB");
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

test("numbers follow the current language", async () => {
  await i18n.changeLanguage("de");

  expect(decibels(-6.04)).toBe("-6,0 dB");
  expect(hertz(12345)).toBe("12,3 kHz");
  expect(formatNumber(1234.5)).toBe("1234,5");
});
