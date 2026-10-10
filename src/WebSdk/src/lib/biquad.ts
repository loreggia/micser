/**
 * Coefficients of a biquad filter, normalized so that a0 is 1.
 */
export interface Biquad {
  b0: number;
  b1: number;
  b2: number;
  a1: number;
  a2: number;
}

function normalize(b0: number, b1: number, b2: number, a0: number, a1: number, a2: number): Biquad {
  return { b0: b0 / a0, b1: b1 / a0, b2: b2 / a0, a1: a1 / a0, a2: a2 / a0 };
}

function omega(sampleRate: number, frequency: number) {
  const w0 = (2 * Math.PI * frequency) / sampleRate;
  return { cos: Math.cos(w0), sin: Math.sin(w0) };
}

/**
 * Peaking EQ (Audio EQ Cookbook), as NAudio's `BiQuadFilter.PeakingEQ`.
 */
export function peakingEq(sampleRate: number, frequency: number, q: number, gainDecibels: number): Biquad {
  const { cos, sin } = omega(sampleRate, frequency);
  const alpha = sin / (2 * q);
  const a = Math.pow(10, gainDecibels / 40);
  return normalize(1 + alpha * a, -2 * cos, 1 - alpha * a, 1 + alpha / a, -2 * cos, 1 - alpha / a);
}

/**
 * Low-pass filter (Audio EQ Cookbook), as NAudio's `BiQuadFilter.LowPassFilter`.
 */
export function lowPass(sampleRate: number, frequency: number, q: number): Biquad {
  const { cos, sin } = omega(sampleRate, frequency);
  const alpha = sin / (2 * q);
  return normalize((1 - cos) / 2, 1 - cos, (1 - cos) / 2, 1 + alpha, -2 * cos, 1 - alpha);
}

/**
 * High-pass filter (Audio EQ Cookbook), as NAudio's `BiQuadFilter.HighPassFilter`.
 */
export function highPass(sampleRate: number, frequency: number, q: number): Biquad {
  const { cos, sin } = omega(sampleRate, frequency);
  const alpha = sin / (2 * q);
  return normalize((1 + cos) / 2, -(1 + cos), (1 + cos) / 2, 1 + alpha, -2 * cos, 1 - alpha);
}

/**
 * The gain in dB of the biquads in series at the frequency.
 */
export function biquadResponse(biquads: readonly Biquad[], sampleRate: number, frequency: number) {
  const w = (2 * Math.PI * frequency) / sampleRate;
  const cos1 = Math.cos(w);
  const sin1 = Math.sin(w);
  const cos2 = Math.cos(2 * w);
  const sin2 = Math.sin(2 * w);
  let decibels = 0;
  for (const { b0, b1, b2, a1, a2 } of biquads) {
    // |H(e^jw)|² with z^-1 = cos w - j sin w
    const numeratorRe = b0 + b1 * cos1 + b2 * cos2;
    const numeratorIm = -(b1 * sin1 + b2 * sin2);
    const denominatorRe = 1 + a1 * cos1 + a2 * cos2;
    const denominatorIm = -(a1 * sin1 + a2 * sin2);
    const power =
      (numeratorRe * numeratorRe + numeratorIm * numeratorIm) /
      (denominatorRe * denominatorRe + denominatorIm * denominatorIm);
    decibels += 10 * Math.log10(Math.max(power, 1e-20));
  }

  return decibels;
}
