import React from "react";
import { DeviceInputWidget } from "./components";

const Plugin = () => {
  const plugin = {
    name: "Main",
    widgets: [
      {
        name: "DeviceInput",
        component: DeviceInputWidget,
      },
    ],
  };

  window.plugins = window.plugins || [];
  window.plugins.push(plugin);
};

Plugin();
