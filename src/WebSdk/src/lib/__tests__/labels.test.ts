import { afterEach, expect, test } from "vitest";
import { i18n } from "../../i18n/i18n";
import {
  decibels,
  formatNumber,
  hertz,
  milliseconds,
  parseHertz,
  parseMilliseconds,
  parseNumber,
  parsePercent,
} from "../labels";

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

test("typed numbers take a decimal point or comma and ignore the unit", () => {
  expect(parseNumber("-3.5 dB")).toBe(-3.5);
  expect(parseNumber("1,25")).toBe(1.25);
  expect(parseNumber("+6")).toBe(6);
  expect(parseNumber("−12")).toBe(-12);
  expect(parseNumber(".5")).toBe(0.5);
  expect(parseNumber("4:1")).toBe(4);
  expect(parseNumber("dB")).toBeUndefined();
  expect(parseNumber("")).toBeUndefined();
});

test("typed frequencies take kilohertz", () => {
  expect(parseHertz("250 Hz")).toBe(250);
  expect(parseHertz("1,5k")).toBe(1500);
  expect(parseHertz("2.4 kHz")).toBe(2400);
});

test("typed times are milliseconds unless they end in seconds", () => {
  expect(parseMilliseconds("10")).toBe(0.01);
  expect(parseMilliseconds("250 ms")).toBe(0.25);
  expect(parseMilliseconds("0,2 s")).toBe(0.2);
});

test("typed percentages are fractions", () => {
  expect(parsePercent("80 %")).toBe(0.8);
  expect(parsePercent("-50")).toBe(-0.5);
});
