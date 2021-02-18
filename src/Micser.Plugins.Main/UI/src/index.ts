import { Plugin } from "micser-common";
import { DeviceInputWidget, DeviceOutputWidget } from "./components";
import { resources } from "./i18n";

const plugin: Plugin = {
    name: "Main",
    widgets: [
        {
            name: "DeviceInput",
            titleResource: "deviceInputWidget.title",
            descriptionResource: "deviceInputWidget.description",
            content: DeviceInputWidget,
            outputHandles: ["Output01"],
        },
        {
            name: "DeviceOutput",
            titleResource: "deviceOutputWidget.title",
            descriptionResource: "deviceOutputWidget.description",
            content: DeviceOutputWidget,
            inputHandles: ["Input01"],
        },
    ],
    resources,
};

export default plugin;
