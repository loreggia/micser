import React from "react";
import { WidgetFC, WidgetProps } from "micser-common";
import { DeviceType, DeviceWidget } from "./DeviceWidget";

export const DeviceOutputWidget: WidgetFC = ({ module }: WidgetProps) => {
    return <DeviceWidget module={module} type={DeviceType.output} />;
};
