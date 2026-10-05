import { definePlugin, defineWidget } from "@micser/web-sdk";
import { createElement } from "react";
import { t } from "./i18n";
import { CompressorWidget } from "./widgets/CompressorWidget";
import { DeviceWidget } from "./widgets/DeviceWidget";
import { EqualizerWidget } from "./widgets/EqualizerWidget";
import { GainWidget } from "./widgets/GainWidget";
import { PitchWidget } from "./widgets/PitchWidget";
import { SpectrumWidget } from "./widgets/SpectrumWidget";

export default definePlugin({
  name: "Main",
  widgets: [
    defineWidget({
      moduleType: "DeviceInput",
      title: () => t("modules.deviceInput"),
      component: (props) => createElement(DeviceWidget, { ...props, direction: "Input" }),
    }),
    defineWidget({
      moduleType: "LoopbackInput",
      title: () => t("modules.loopbackInput"),
      component: (props) => createElement(DeviceWidget, { ...props, direction: "Output" }),
    }),
    defineWidget({
      moduleType: "DeviceOutput",
      title: () => t("modules.deviceOutput"),
      component: (props) => createElement(DeviceWidget, { ...props, direction: "Output" }),
    }),
    defineWidget({ moduleType: "Gain", title: () => t("modules.gain"), component: GainWidget }),
    defineWidget({ moduleType: "Compressor", title: () => t("modules.compressor"), component: CompressorWidget }),
    defineWidget({ moduleType: "Equalizer", title: () => t("modules.equalizer"), component: EqualizerWidget }),
    defineWidget({ moduleType: "Pitch", title: () => t("modules.pitch"), component: PitchWidget }),
    defineWidget({ moduleType: "Spectrum", title: () => t("modules.spectrum"), component: SpectrumWidget }),
  ],
});
