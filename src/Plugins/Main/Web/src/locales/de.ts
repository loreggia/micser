import type { Translations } from "@micser/web-sdk";
import type { en } from "./en";

export const de: Translations<typeof en> = {
  modules: {
    deviceInput: "Eingabegerät",
    loopbackInput: "Loopback",
    deviceOutput: "Ausgabegerät",
    gain: "Verstärkung",
    compressor: "Kompressor",
    equalizer: "Equalizer",
    filter: "Filter",
    pitch: "Tonhöhe",
    spectrum: "Spektrum",
  },
  device: {
    none: "Kein Gerät",
    unavailable: "Nicht verfügbar",
    unavailableAdapter: "Nicht verfügbar ({{adapter}})",
    deviceUnavailable: "{{name}} (nicht verfügbar)",
    notRunning: "Läuft nicht",
    running: "Läuft",
    dropouts_one: "{{count}} Aussetzer",
    dropouts_other: "{{count}} Aussetzer",
    buffer: "{{status}} · {{milliseconds}} ms Puffer",
  },
  compressor: {
    downward: "Abwärts",
    upward: "Aufwärts",
    threshold: "Schwelle",
    ratio: "Verhältnis",
    attack: "Attack",
    release: "Release",
    knee: "Knie",
    makeUpGain: "Make-up-Gain",
    amount: "Anteil",
  },
  equalizer: {
    band: "Band {{number}}",
    removeBand: "Band {{number}} entfernen",
    frequency: "Frequenz",
    gain: "Verstärkung",
    q: "Güte",
    addBand: "Band hinzufügen",
    response: "Frequenzgang",
  },
  filter: {
    highPass: "Hochpass",
    lowPass: "Tiefpass",
    frequency: "Frequenz",
    slope: "Flankensteilheit",
    q: "Güte",
    decibelsPerOctave: "{{value}} dB/Okt.",
    response: "Frequenzgang",
  },
  gain: {
    gain: "Verstärkung",
  },
  pitch: {
    pitch: "Tonhöhe",
    semitones: "{{value}} HT",
    quality: "Qualität",
  },
};
