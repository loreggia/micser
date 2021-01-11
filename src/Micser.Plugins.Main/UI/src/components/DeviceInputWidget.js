import React from "react";
import { Select } from "antd";
import { Loader, useApi } from "micser-common";
import { DeviceSelect, WidgetContainer } from "./Common";

const { Option } = Select;

const DeviceInputWidget = ({ data }) => {
    const { result: devices, isLoading } = useApi("Devices/Input");

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

export default DeviceInputWidget;
