import React, { FC, useContext } from "react";
import { Select } from "antd";
import { SelectValue } from "antd/lib/select";
import { CommonControls, Contexts, Loader, useGetApi, WidgetProps } from "micser-common";

import { DeviceDescription, DeviceSelect, WidgetContainer } from "./Common";

const { Option } = Select;

export enum DeviceType {
    input = "Input",
    output = "Output",
}

export interface DeviceWidgetProps extends WidgetProps {
    type: DeviceType;
}

export const DeviceWidget: FC<DeviceWidgetProps> = ({ module, type }) => {
    const [devices, { isLoading }] = useGetApi<DeviceDescription[]>(`Devices/${type}`);

    const dashboardContext = useContext(Contexts.dashboard);

    const handleDeviceChange = (value: SelectValue) => {
        dashboardContext.onStateChanged(module, { deviceId: value?.toString() });
    };

    return (
        <WidgetContainer>
            <Loader isVisible={isLoading} />
            <CommonControls module={module} />
            <DeviceSelect dropdownMatchSelectWidth={false} value={module.state.deviceId} onChange={handleDeviceChange}>
                {devices &&
                    devices.map((device) => (
                        <Option key={device.id} value={device.id}>
                            {device.friendlyName}
                        </Option>
                    ))}
            </DeviceSelect>
        </WidgetContainer>
    );
};
