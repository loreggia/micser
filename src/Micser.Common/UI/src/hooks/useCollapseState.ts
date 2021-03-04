import { useCallback, useContext, useEffect, useState } from "react";
import { Contexts } from "Contexts";
import { Module } from "models";

type CollapseKeys = string | string[];

export const useCollapseState = (
    module: Module,
    defaultActiveKeys?: CollapseKeys
): [CollapseKeys, (keys: CollapseKeys) => void] => {
    const dashboardContext = useContext(Contexts.dashboard);

    const [activeKeys, setActiveKeys] = useState<CollapseKeys>(defaultActiveKeys || []);

    useEffect(() => {
        if (module.state.activeCollapseKeys) {
            try {
                const stateKeys = JSON.parse(module.state.activeCollapseKeys);
                setActiveKeys(stateKeys);
            } catch {
                // ignored
            }
        }
    }, [module.state.activeCollapseKeys]);

    const handleChange = useCallback(
        (keys: CollapseKeys) => {
            setActiveKeys(keys);
            const value = JSON.stringify(keys);
            dashboardContext.onStateChanged(module, { activeCollapseKeys: value });
        },
        [module, dashboardContext.onStateChanged]
    );

    return [activeKeys, handleChange];
};
