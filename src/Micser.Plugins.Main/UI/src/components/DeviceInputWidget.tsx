import React from "react";
import { WidgetFC } from "micser-common";
import { DeviceType, DeviceWidget } from "./DeviceWidget";

export const DeviceInputWidget: WidgetFC = ({ module }) => {
    return <DeviceWidget module={module} type={DeviceType.input} />;
};
