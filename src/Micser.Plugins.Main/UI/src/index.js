import { DeviceInputWidget, DeviceOutputWidget } from "./components";

const Plugin = () => {
    return {
        name: "Main",
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
    };
};

export default Plugin;
