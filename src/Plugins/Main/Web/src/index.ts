import { defineWidget, type Plugin } from "@micser/web-sdk";
import { createElement } from "react";
import { DeviceWidget } from "./widgets/DeviceWidget";
import { SpectrumWidget } from "./widgets/SpectrumWidget";
import { GainWidget } from "./widgets/GainWidget";
import { CompressorWidget } from "./widgets/CompressorWidget";
import { EqualizerWidget } from "./widgets/EqualizerWidget";
import { PitchWidget } from "./widgets/PitchWidget";

export const mainPlugin: Plugin = {
  name: "Main",
  widgets: [
    defineWidget({
      moduleType: "DeviceInput",
      title: "Input device",
      component: (props) => createElement(DeviceWidget, { ...props, direction: "Input" }),
    }),
    defineWidget({
      moduleType: "LoopbackInput",
      title: "Loopback",
      component: (props) => createElement(DeviceWidget, { ...props, direction: "Output" }),
    }),
    defineWidget({
      moduleType: "DeviceOutput",
      title: "Output device",
      component: (props) => createElement(DeviceWidget, { ...props, direction: "Output" }),
    }),
    defineWidget({ moduleType: "Gain", title: "Gain", component: GainWidget }),
    defineWidget({ moduleType: "Compressor", title: "Compressor", component: CompressorWidget }),
    defineWidget({ moduleType: "Equalizer", title: "Equalizer", component: EqualizerWidget }),
    defineWidget({ moduleType: "Pitch", title: "Pitch", component: PitchWidget }),
    defineWidget({ moduleType: "Spectrum", title: "Spectrum", component: SpectrumWidget }),
  ],
};
