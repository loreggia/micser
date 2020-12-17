import React from "react";
import { DeviceInputWidget } from "./components";

const Plugin = () => {
  return {
    name: "Main",
    widgets: [
      {
        name: "DeviceInput",
        component: DeviceInputWidget,
      },
    ],
  };
};

export default Plugin;
