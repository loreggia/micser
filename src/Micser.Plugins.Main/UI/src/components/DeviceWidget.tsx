import React, { FC, useContext } from "react";
import { useTranslation } from "react-i18next";
import { Collapse, Select } from "antd";
import { SelectValue } from "antd/lib/select";
import {
    VolumeControls,
    Contexts,
    DeviceDescription,
    Loader,
    useCollapseState,
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

    const [activeCollapseKeys, onCollapseChange] = useCollapseState(module, ["common-controls", "device"]);

    const dashboardContext = useContext(Contexts.dashboard);

    const handleDeviceChange = (value: SelectValue) => {
        dashboardContext.onStateChanged(module, { deviceId: value?.toString() });
    };

    return (
        <>
            <Loader isVisible={isLoading} />
            <Collapse activeKey={activeCollapseKeys} onChange={onCollapseChange}>
                <WidgetPanel key="common-controls" header={t("widgets.volumeControls.title")}>
                    <VolumeControls module={module} />
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
