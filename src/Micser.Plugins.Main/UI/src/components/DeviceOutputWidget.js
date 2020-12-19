import React, { useCallback, useEffect, useState } from "react";
import { useApi } from "../../../../Micser/App/src/hooks";

const DeviceOutputWidget = ({ data }) => {
    const [devices, setDevices] = useState([]);
    const [devicesApi, isLoadingDevicesApi] = useApi("devices");

    const loadDevices = useCallback(async () => {
        const result = await devicesApi({ action: "Output" });
        if (result.data) {
            setDevices(result.data);
        }
    }, [devicesApi]);

    useEffect(() => {
        loadDevices();
    }, []);

    return (
        <div>
            {devices.map((d, i) => (
                <p key={i}>
                    {d.friendlyName}
                    <br />
                </p>
            ))}
        </div>
    );
};

export default DeviceOutputWidget;
