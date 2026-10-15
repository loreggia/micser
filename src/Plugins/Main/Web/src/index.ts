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
import { LimiterWidget } from "./widgets/LimiterWidget";
import { NoiseGateWidget } from "./widgets/NoiseGateWidget";
import { PitchWidget } from "./widgets/PitchWidget";
import { SpectrumWidget } from "./widgets/SpectrumWidget";
import { StereoToolsWidget } from "./widgets/StereoToolsWidget";

export default definePlugin({
  name: "Main",
  widgets: [
    defineWidget({
      moduleType: "DeviceInput",
      title: () => t("modules.deviceInput"),
      description: () => t("descriptions.deviceInput"),
      component: (props) => createElement(DeviceWidget, { ...props, direction: "Input" }),
    }),
    defineWidget({
      moduleType: "LoopbackInput",
      title: () => t("modules.loopbackInput"),
      description: () => t("descriptions.loopbackInput"),
      component: (props) => createElement(DeviceWidget, { ...props, direction: "Output" }),
    }),
    defineWidget({
      moduleType: "DeviceOutput",
      title: () => t("modules.deviceOutput"),
      description: () => t("descriptions.deviceOutput"),
      component: (props) => createElement(DeviceWidget, { ...props, direction: "Output" }),
    }),
    defineWidget({
      moduleType: "Gain",
      title: () => t("modules.gain"),
      description: () => t("descriptions.gain"),
      component: GainWidget,
    }),
    defineWidget({
      moduleType: "Compressor",
      title: () => t("modules.compressor"),
      description: () => t("descriptions.compressor"),
      component: CompressorWidget,
    }),
    defineWidget({
      moduleType: "NoiseGate",
      title: () => t("modules.noiseGate"),
      description: () => t("descriptions.noiseGate"),
      component: NoiseGateWidget,
    }),
    defineWidget({
      moduleType: "Ducker",
      title: () => t("modules.ducker"),
      description: () => t("descriptions.ducker"),
      component: DuckerWidget,
    }),
    defineWidget({
      moduleType: "Delay",
      title: () => t("modules.delay"),
      description: () => t("descriptions.delay"),
      component: DelayWidget,
    }),
    defineWidget({
      moduleType: "StereoTools",
      title: () => t("modules.stereoTools"),
      description: () => t("descriptions.stereoTools"),
      component: StereoToolsWidget,
    }),
    defineWidget({
      moduleType: "Limiter",
      title: () => t("modules.limiter"),
      description: () => t("descriptions.limiter"),
      component: LimiterWidget,
    }),
    defineWidget({
      moduleType: "Equalizer",
      title: () => t("modules.equalizer"),
      description: () => t("descriptions.equalizer"),
      component: EqualizerWidget,
    }),
    defineWidget({
      moduleType: "Filter",
      title: () => t("modules.filter"),
      description: () => t("descriptions.filter"),
      component: FilterWidget,
    }),
    defineWidget({
      moduleType: "Pitch",
      title: () => t("modules.pitch"),
      description: () => t("descriptions.pitch"),
      component: PitchWidget,
    }),
    defineWidget({
      moduleType: "Spectrum",
      title: () => t("modules.spectrum"),
      description: () => t("descriptions.spectrum"),
      component: SpectrumWidget,
    }),
  ],
});
