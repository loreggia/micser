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
