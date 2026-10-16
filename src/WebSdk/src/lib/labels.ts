import { currentLanguage } from "../i18n/i18n";

/**
 * Formats a number for the current language, e.g. with a decimal comma in German.
 */
export const formatNumber = (value: number, options?: Intl.NumberFormatOptions) =>
  new Intl.NumberFormat(currentLanguage(), { useGrouping: false, ...options }).format(value);

export const decibels = (value: number) =>
  `${formatNumber(value, { minimumFractionDigits: 1, maximumFractionDigits: 1, signDisplay: "exceptZero" })} dB`;

export const milliseconds = (seconds: number) => `${formatNumber(seconds * 1000, { maximumSignificantDigits: 3 })} ms`;

export const hertz = (value: number) =>
  value >= 1000
    ? `${formatNumber(value / 1000, { maximumSignificantDigits: 3 })} kHz`
    : `${formatNumber(value, { maximumFractionDigits: 0 })} Hz`;

/**
 * Reads a number someone typed, with a decimal point or comma, ignoring a unit after it; undefined without a number.
 */
export function parseNumber(text: string): number | undefined {
  const match = /^\s*([+\-−]?)\s*(\d*[.,]?\d+)/.exec(text);
  if (!match) {
    return undefined;
  }

  const value = Number(match[2].replace(",", "."));
  return match[1] && match[1] !== "+" ? -value : value;
}

/** Reads a frequency in hertz, or in kilohertz with a "k" after the number ("1.5k", "1,5 kHz"). */
export function parseHertz(text: string): number | undefined {
  const value = parseNumber(text);
  return value !== undefined && /\d\s*k/i.test(text) ? value * 1000 : value;
}

/** Reads a time typed in milliseconds, or in seconds with an "s" after the number, as seconds. */
export function parseMilliseconds(text: string): number | undefined {
  const value = parseNumber(text);
  if (value === undefined) {
    return undefined;
  }

  return /\d\s*s\s*$/i.test(text) ? value : value / 1000;
}

/** Reads a percentage as a fraction: "80" and "80 %" are 0.8. */
export function parsePercent(text: string): number | undefined {
  const value = parseNumber(text);
  return value === undefined ? undefined : value / 100;
}
