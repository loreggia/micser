import React, { FC, useContext, useEffect, useState } from "react";
import { Checkbox, Collapse, Slider } from "antd";

import { Module } from "models";
import { Contexts } from "Contexts";
import { parseBool } from "utils";
import { CheckboxChangeEvent } from "antd/lib/checkbox";

export interface CommonControlsProps {
    module: Module;
}

export const CommonControls: FC<CommonControlsProps> = ({ module }) => {
    const dashboardContext = useContext(Contexts.dashboard);

    const [volume, setVolume] = useState(100);
    const [isMuted, setIsMuted] = useState(false);
    const [useSystemVolume, setUseSystemVolume] = useState(false);

    useEffect(() => {
        const parsed = parseInt(module.state.volume);
        if (!isNaN(parsed)) {
            setVolume(parsed);
        }
    }, [module.state.volume]);

    useEffect(() => {
        const parsed = parseBool(module.state.isMuted);
        setIsMuted(parsed);
    }, [module.state.isMuted]);

    useEffect(() => {
        const parsed = parseBool(module.state.useSystemVolume);
        setUseSystemVolume(parsed);
    }, [module.state.useSystemVolume]);

    const handleVolumeChange = (value: number) => {
        setVolume(value);
        dashboardContext?.onStateChanged(module, { volume: value.toString() });
    };

    const handleIsMutedChange = (e: CheckboxChangeEvent) => {
        const value = e.target.checked;
        setIsMuted(value);
        dashboardContext?.onStateChanged(module, { isMuted: value.toString() });
    };

    const handleUseSystemVolumeChange = (e: CheckboxChangeEvent) => {
        const value = e.target.checked;
        setUseSystemVolume(e.target.checked);
        dashboardContext?.onStateChanged(module, { useSystemVolume: value.toString() });
    };

    return (
        <Collapse defaultActiveKey={["common-controls"]}>
            <Collapse.Panel header="Common Controls" key="common-controls">
                <Slider min={0} max={100} value={volume} onChange={handleVolumeChange} />
                <Checkbox checked={isMuted} onChange={handleIsMutedChange}>
                    Mute
                </Checkbox>
                <Checkbox checked={useSystemVolume} onChange={handleUseSystemVolumeChange}>
                    Use system volume
                </Checkbox>
            </Collapse.Panel>
        </Collapse>
    );
};
