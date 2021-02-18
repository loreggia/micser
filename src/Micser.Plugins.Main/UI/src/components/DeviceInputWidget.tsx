import React from "react";
import { Select } from "antd";
import { useTranslation } from "react-i18next";
import { Loader, useGetApi, WidgetFC } from "micser-common";

import { DeviceDescription, DeviceSelect, WidgetContainer } from "./Common";

const { Option } = Select;

export const DeviceInputWidget: WidgetFC = ({ data }) => {
    const [devices, { isLoading }] = useGetApi<DeviceDescription[]>("Devices/Input");
    const { t } = useTranslation("plugins.main");

    return (
        <WidgetContainer>
            {t("deviceInputWidget.title")}
            <Loader isVisible={isLoading} />
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
