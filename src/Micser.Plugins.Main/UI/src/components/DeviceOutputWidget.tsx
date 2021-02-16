import React from "react";
import { Select } from "antd";
import { Loader, useGetApi, WidgetFC } from "micser-common";
import { DeviceDescription, DeviceSelect, WidgetContainer } from "./Common";

const { Option } = Select;

export const DeviceOutputWidget: WidgetFC = ({ data }) => {
    const [devices, { isLoading }] = useGetApi<DeviceDescription[]>("Devices/Output");

    return (
        <WidgetContainer>
            {isLoading && <Loader />}
            <DeviceSelect defaultValue={data.state.deviceId} dropdownMatchSelectWidth={false}>
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
