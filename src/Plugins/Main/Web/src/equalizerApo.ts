import type { EqualizerBand, EqualizerBandType, EqualizerState } from "@micser/web-sdk";

/** The engine's EqualizerModule.MaxBands. */
export const maxBands = 32;

export interface ImportedEqualizer extends EqualizerState {
  /** Filters that were left out: of a type the equalizer doesn't have, for single channels, or beyond the band limit. */
  skipped: number;
}

const types: Record<string, EqualizerBandType> = {
  PK: "Peaking",
  PEQ: "Peaking",
  LS: "LowShelf",
  LSC: "LowShelf",
  HS: "HighShelf",
  HSC: "HighShelf",
  LP: "LowPass",
  LPQ: "LowPass",
  HP: "HighPass",
  HPQ: "HighPass",
};

const clamp = (value: number, min: number, max: number) => Math.min(Math.max(value, min), max);

function parseNumber(text: string | undefined) {
  const value = Number(text?.replace(",", "."));
  return Number.isFinite(value) ? value : undefined;
}

/** The Q of a band that is the given number of octaves wide. */
function bandwidthToQ(octaves: number) {
  const ratio = Math.pow(2, octaves);
  return Math.sqrt(ratio) / (ratio - 1);
}

/**
 * Reads an Equalizer APO configuration (config.txt), e.g. an AutoEQ "ParametricEQ.txt": the preamp and the filters that apply to all
 * channels. Shelves and low/high-pass filters without a Q get a Butterworth Q (0.71). Values are limited to the equalizer's ranges.
 * Returns undefined if the text has neither a filter nor a preamp.
 */
export function parseEqualizerApo(text: string): ImportedEqualizer | undefined {
  const bands: EqualizerBand[] = [];
  let preamp: number | undefined;
  let skipped = 0;
  let allChannels = true;

  for (const rawLine of text.split(/\r?\n/)) {
    const line = rawLine.replace(/#.*$/, "").trim();
    const [, command, rest] = /^([A-Za-z]+)\s*\d*\s*:\s*(.*)$/.exec(line) ?? [];
    switch (command?.toLowerCase()) {
      case "preamp": {
        const value = parseNumber(/^(-?[\d.,]+)/.exec(rest)?.[1]);
        if (value !== undefined && allChannels) {
          preamp = (preamp ?? 0) + value;
        }

        break;
      }

      case "channel":
        allChannels = rest.trim().toLowerCase() === "all";
        break;

      case "filter": {
        const tokens = rest.split(/\s+/);
        if (tokens[0]?.toUpperCase() !== "ON") {
          break;
        }

        const type = types[tokens[1]?.toUpperCase()];
        const value = (name: string) => {
          const index = tokens.findIndex((token) => token.toLowerCase() === name);
          // "BW Oct 1" has the unit before the number
          const next = tokens[index + 1]?.toLowerCase() === "oct" ? index + 2 : index + 1;
          return index < 0 ? undefined : parseNumber(tokens[next]);
        };
        const frequency = value("fc");
        if (!type || frequency === undefined || !allChannels || bands.length >= maxBands) {
          skipped++;
          break;
        }

        const bandwidth = value("bw");
        const q =
          value("q") ?? (bandwidth !== undefined ? bandwidthToQ(bandwidth) : type === "Peaking" ? 1.41 : Math.SQRT1_2);
        bands.push({
          type,
          frequency: clamp(frequency, 20, 20000),
          gain: clamp(value("gain") ?? 0, -24, 24),
          q: clamp(q, 0.1, 20),
        });
        break;
      }
    }
  }

  if (bands.length === 0 && preamp === undefined) {
    return undefined;
  }

  return { bands, preamp: clamp(preamp ?? 0, -24, 24), skipped };
}
