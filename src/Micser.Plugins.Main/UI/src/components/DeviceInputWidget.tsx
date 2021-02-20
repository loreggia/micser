import React, { useContext } from "react";
import { Select } from "antd";
import { SelectValue } from "antd/lib/select";
import { Contexts, Loader, ModuleState, useGetApi, WidgetFC } from "micser-common";

import { DeviceDescription, DeviceSelect, WidgetContainer } from "./Common";

const { Option } = Select;

export const DeviceInputWidget: WidgetFC = ({ module }) => {
    const [devices, { isLoading }] = useGetApi<DeviceDescription[]>("Devices/Input");

    const dashboardContext = useContext(Contexts.dashboard);

    const handleDeviceChange = (value: SelectValue) => {
        const state = {
            ...module.state,
            deviceId: value?.toString(),
        } as ModuleState;
        dashboardContext.onStateChanged(module, state);
    };

    return (
        <WidgetContainer>
            <Loader isVisible={isLoading} />
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
