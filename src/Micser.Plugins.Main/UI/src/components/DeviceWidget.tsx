import React, { FC, useContext } from "react";
import { useTranslation } from "react-i18next";
import { Collapse, Select } from "antd";
import { SelectValue } from "antd/lib/select";
import {
    CommonControls,
    Contexts,
    DeviceDescription,
    Loader,
    useGetApi,
    WidgetPanel,
    WidgetProps,
} from "micser-common";

const { Option } = Select;

export enum DeviceType {
    input = "Input",
    output = "Output",
}

export interface DeviceWidgetProps extends WidgetProps {
    type: DeviceType;
}

export const DeviceWidget: FC<DeviceWidgetProps> = ({ module, type }) => {
    const { t } = useTranslation();
    const [devices, { isLoading }] = useGetApi<DeviceDescription[]>(`Devices/${type}`);

    const dashboardContext = useContext(Contexts.dashboard);

    const handleDeviceChange = (value: SelectValue) => {
        dashboardContext.onStateChanged(module, { deviceId: value?.toString() });
    };

    return (
        <>
            <Loader isVisible={isLoading} />
            <Collapse defaultActiveKey="common-controls">
                <WidgetPanel key="common-controls" header={t("widgets.commonControls.title")}>
                    <CommonControls module={module} />
                </WidgetPanel>
                <WidgetPanel key="device" header={t("common.device")}>
                    <Select
                        dropdownMatchSelectWidth={false}
                        value={module.state.deviceId}
                        onChange={handleDeviceChange}
                        style={{ width: "100%" }}
                    >
                        {devices &&
                            devices.map((device) => (
                                <Option key={device.id} value={device.id}>
                                    {device.friendlyName}
                                </Option>
                            ))}
                    </Select>
                </WidgetPanel>
            </Collapse>
        </>
    );
};
