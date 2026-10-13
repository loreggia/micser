import { definePlugin, defineWidget } from "@micser/web-sdk";
import { createElement } from "react";
import { t } from "./i18n";
import { CompressorWidget } from "./widgets/CompressorWidget";
import { DelayWidget } from "./widgets/DelayWidget";
import { DeviceWidget } from "./widgets/DeviceWidget";
import { DuckerWidget } from "./widgets/DuckerWidget";
import { EqualizerWidget } from "./widgets/EqualizerWidget";
import { FilterWidget } from "./widgets/FilterWidget";
import { GainWidget } from "./widgets/GainWidget";
import { NoiseGateWidget } from "./widgets/NoiseGateWidget";
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
    defineWidget({ moduleType: "NoiseGate", title: () => t("modules.noiseGate"), component: NoiseGateWidget }),
    defineWidget({ moduleType: "Ducker", title: () => t("modules.ducker"), component: DuckerWidget }),
    defineWidget({ moduleType: "Delay", title: () => t("modules.delay"), component: DelayWidget }),
    defineWidget({ moduleType: "Equalizer", title: () => t("modules.equalizer"), component: EqualizerWidget }),
    defineWidget({ moduleType: "Filter", title: () => t("modules.filter"), component: FilterWidget }),
    defineWidget({ moduleType: "Pitch", title: () => t("modules.pitch"), component: PitchWidget }),
    defineWidget({ moduleType: "Spectrum", title: () => t("modules.spectrum"), component: SpectrumWidget }),
  ],
});
