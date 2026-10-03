export const decibels = (value: number) => `${value > 0 ? "+" : ""}${value.toFixed(1)} dB`;

export const milliseconds = (seconds: number) => `${Number((seconds * 1000).toPrecision(3))} ms`;

export const hertz = (value: number) =>
  value >= 1000 ? `${Number((value / 1000).toPrecision(3))} kHz` : `${Math.round(value)} Hz`;
