import React from "react";
import styled from "styled-components";
import { Select } from "antd";
import { Loader } from "../../../../Micser/App/src/components";
import { useApi } from "../../../../Micser/App/src/hooks";

const { Option } = Select;

const Container = styled.div`
    position: relative;
    min-width: 100px;
    max-width: 500px;
`;

const DeviceInputWidget = ({ data }) => {
    const [devices, isLoading] = useApi("Devices/Input");

    return (
        <Container>
            {isLoading && <Loader />}
            <Select defaultValue={data.state.deviceId}>
                {devices &&
                    devices.map((device) => (
                        <Option key={device.id} value={device.id}>
                            {device.friendlyName}
                        </Option>
                    ))}
            </Select>
        </Container>
    );
};

export default DeviceInputWidget;
