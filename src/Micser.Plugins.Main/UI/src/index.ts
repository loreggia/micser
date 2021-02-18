import { Plugin } from "micser-common";
import { DeviceInputWidget, DeviceOutputWidget } from "./components";
import { resources } from "./i18n";

const plugin: Plugin = {
    name: "main",
    widgets: [
        {
            name: "DeviceInput",
            content: DeviceInputWidget,
            outputHandles: ["Output01"],
        },
        {
            name: "DeviceOutput",
            content: DeviceOutputWidget,
            inputHandles: ["Input01"],
        },
    ],
    resources,
};

export default plugin;
