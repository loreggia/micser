import React, { useCallback, useEffect, useState } from "react";
import styled from "styled-components";
import { Select, Spin } from "antd";
import { useApi } from "../../../../Micser/App/src/hooks";

const { Option } = Select;

const Container = styled.div`
    min-width: 100px;
    max-width: 500px;
    display: flex;
    justify-content: center;
    align-items: center;
`;

const DeviceInputWidget = ({ data }) => {
    const [devices, setDevices] = useState([]);
    const [devicesApi, isLoadingDevicesApi] = useApi("devices");

    const loadDevices = useCallback(
        async (isMounted) => {
            const result = await devicesApi({ action: "Input", isCancelled: () => !isMounted() });
            if (isMounted() && result && result.data) {
                setDevices(result.data);
            }
        },
        [devicesApi]
    );

    useEffect(() => {
        let isMounted = true;
        loadDevices(() => isMounted);
        return () => (isMounted = false);
    }, []);

    return (
        <Container>
            {isLoadingDevicesApi ? (
                <Spin />
            ) : (
                <Select defaultValue={data.state.deviceId}>
                    {devices.map((device) => (
                        <Option key={device.id} value={device.id}>
                            {device.friendlyName}
                        </Option>
                    ))}
                </Select>
            )}
        </Container>
    );
};

export default DeviceInputWidget;
