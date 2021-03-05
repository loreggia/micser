import React, { FC, useContext, useEffect, useState } from "react";
import { Checkbox, Col, InputNumber, Row, Slider } from "antd";
import { CheckboxChangeEvent } from "antd/lib/checkbox";
import { useTranslation } from "react-i18next";

import { Module } from "../../models";
import { Contexts } from "../../Contexts";
import { parseBool } from "../../utils";

export interface VolumeControlsProps {
    module: Module;
}

export const VolumeControls: FC<VolumeControlsProps> = ({ module }: VolumeControlsProps) => {
    const { t } = useTranslation();

    const dashboardContext = useContext(Contexts.dashboard);

    const [volume, setVolume] = useState(100);
    const [isMuted, setIsMuted] = useState(false);
    const [useSystemVolume, setUseSystemVolume] = useState(false);

    useEffect(() => {
        const parsed = parseFloat(module.state.volume);
        if (!isNaN(parsed)) {
            setVolume(parsed * 100);
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

    const handleVolumeChange = (value: number, skipSaving?: boolean) => {
        setVolume(value);
        if (!skipSaving) {
            dashboardContext?.onStateChanged(module, { volume: (value / 100).toString() });
        }
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

    const stopPropagationHandler = (e: React.SyntheticEvent) => {
        // stop event propagation to enable correct slider inside the draggable widget
        e.stopPropagation();
    };

    return (
        <>
            <Row>
                <Col span={24}>{t("widgets.volumeControls.volume")}</Col>
                <Col span={16} onMouseDown={stopPropagationHandler} onTouchStart={stopPropagationHandler}>
                    <Slider
                        min={0}
                        max={100}
                        value={volume}
                        onChange={(value: number) => handleVolumeChange(value, true)}
                        onAfterChange={handleVolumeChange}
                        tipFormatter={(value) => `${value}%`}
                        step={1}
                    />
                </Col>
                <Col span={8}>
                    <InputNumber
                        min={0}
                        max={100}
                        value={volume}
                        onChange={handleVolumeChange}
                        step={1}
                        style={{ float: "right" }}
                    />
                </Col>
            </Row>
            <Row>
                <Col>
                    <Checkbox checked={isMuted} onChange={handleIsMutedChange}>
                        {t("widgets.volumeControls.isMuted")}
                    </Checkbox>
                    <Checkbox checked={useSystemVolume} onChange={handleUseSystemVolumeChange}>
                        {t("widgets.volumeControls.useSystemVolume")}
                    </Checkbox>
                </Col>
            </Row>
        </>
    );
};
