import React, { useState, useEffect, createContext, useContext, useCallback } from 'react';
import styled from 'styled-components';
import { Spin, Row, Col, Slider, InputNumber, Checkbox, Collapse } from 'antd';
import { useTranslation } from 'react-i18next';
import AxiosStatic from 'axios';
import { trimStart } from 'lodash';

const Container = styled.div `
    position: absolute;
    width: 100%;
    height: 100%;
    display: flex;
    justify-content: center;
    align-items: center;
    background-color: rgba(0, 0, 0, 0.5);
    z-index: 999;
`;
const Loader = ({ isVisible, tip, suspenseTime }) => {
    const [isVisibleInternal, setIsVisibleInternal] = useState(false);
    useEffect(() => {
        let timeout;
        if (isVisible) {
            const handler = () => {
                setIsVisibleInternal(true);
            };
            timeout = setTimeout(handler, suspenseTime);
        }
        else {
            clearTimeout(timeout);
            setIsVisibleInternal(false);
        }
        return () => {
            clearTimeout(timeout);
        };
    }, [isVisible]);
    return isVisibleInternal ? (React.createElement(Container, null,
        React.createElement(Spin, { tip: tip }))) : null;
};
Loader.defaultProps = {
    isVisible: true,
    tip: "Loading...",
    suspenseTime: 1000,
};

const Contexts = {
    widgetTypes: createContext([]),
    dashboard: createContext({
        onStateChanged: () => { },
    }),
};

const parseBool = (value) => {
    return value && value.toLowerCase() === "true";
};

const VolumeControls = ({ module }) => {
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
    const handleVolumeChange = (value, skipSaving) => {
        setVolume(value);
        if (!skipSaving) {
            dashboardContext?.onStateChanged(module, { volume: (value / 100).toString() });
        }
    };
    const handleIsMutedChange = (e) => {
        const value = e.target.checked;
        setIsMuted(value);
        dashboardContext?.onStateChanged(module, { isMuted: value.toString() });
    };
    const handleUseSystemVolumeChange = (e) => {
        const value = e.target.checked;
        setUseSystemVolume(e.target.checked);
        dashboardContext?.onStateChanged(module, { useSystemVolume: value.toString() });
    };
    const stopPropagationHandler = (e) => {
        // stop event propagation to enable correct slider inside the draggable widget
        e.stopPropagation();
    };
    return (React.createElement(React.Fragment, null,
        React.createElement(Row, null,
            React.createElement(Col, { span: 24 }, t("widgets.volumeControls.volume")),
            React.createElement(Col, { span: 16, onMouseDown: stopPropagationHandler, onTouchStart: stopPropagationHandler },
                React.createElement(Slider, { min: 0, max: 100, value: volume, onChange: (value) => handleVolumeChange(value, true), onAfterChange: handleVolumeChange, tipFormatter: (value) => `${value}%`, step: 1 })),
            React.createElement(Col, { span: 8 },
                React.createElement(InputNumber, { min: 0, max: 100, value: volume, onChange: handleVolumeChange, step: 1, style: { float: "right" } }))),
        React.createElement(Row, null,
            React.createElement(Col, null,
                React.createElement(Checkbox, { checked: isMuted, onChange: handleIsMutedChange }, t("widgets.volumeControls.isMuted")),
                React.createElement(Checkbox, { checked: useSystemVolume, onChange: handleUseSystemVolumeChange }, t("widgets.volumeControls.useSystemVolume"))))));
};

const WidgetPanel = styled(Collapse.Panel) ``;

class Api {
    constructor(baseUrl, onBeginRequest, onEndRequest, onError) {
        this._axios = AxiosStatic.create({
            withCredentials: true,
            baseURL: "/api/" + trimStart(baseUrl, "/"),
        });
        this._onBeginRequest = onBeginRequest;
        this._onEndRequest = onEndRequest;
        this._onError = onError;
    }
    async get(action = "", params) {
        return this.execute(() => this._axios.get(action, { params }));
    }
    async getList(action = "", params) {
        return this.execute(() => this._axios.get(action, { params }));
    }
    async post(action = "", data) {
        return this.execute(() => this._axios.post(action, data));
    }
    async put(action, data) {
        return this.execute(() => this._axios.put(`${action}`, data));
    }
    async delete(action, params) {
        return this.execute(() => this._axios.delete(`${action}`, { params }));
    }
    async execute(func) {
        try {
            this._onBeginRequest();
            const result = await func();
            return this.getResult(result);
        }
        catch (error) {
            return this.handleError(error);
        }
        finally {
            this._onEndRequest();
        }
    }
    getResult(axiosResult) {
        const isSuccess = axiosResult.status >= 200 && axiosResult.status < 400;
        return {
            isSuccess,
            problem: isSuccess ? undefined : axiosResult.data,
            data: isSuccess ? axiosResult.data : undefined,
        };
    }
    handleError(error) {
        console.log(error);
        const problem = error.response && error.response.data
            ? error.response.data
            : {
                type: "Internal",
                status: 500,
                title: "Unknown error.",
            };
        this._onError(problem);
        return {
            isSuccess: false,
            problem,
        };
    }
}

function useApi(path) {
    const [api, setApi] = useState();
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState();
    useEffect(() => {
        let canceled = false;
        const onError = (error) => {
            if (!canceled) {
                setError(error);
            }
        };
        const onRequestAction = (isLoading) => {
            if (!canceled) {
                setIsLoading(isLoading);
            }
        };
        const api = new Api(path, () => onRequestAction(true), () => onRequestAction(false), onError);
        setApi(api);
        return () => {
            canceled = true;
        };
    }, []);
    return [api, { isLoading, error }];
}

const useGetApi = (path, action, params) => {
    const [api, { isLoading, error }] = useApi(path);
    const [result, setResult] = useState(null);
    const [refreshIndex, setRefreshIndex] = useState(0);
    useEffect(() => {
        let canceled = false;
        const loadData = async () => {
            if (api) {
                const result = await api.get(action || "", params);
                if (!canceled && result.isSuccess) {
                    setResult(result.data || null);
                }
            }
        };
        loadData();
        return () => {
            canceled = true;
        };
    }, [api, params, refreshIndex]);
    const refresh = useCallback(() => {
        setRefreshIndex((i) => i + 1);
    }, []);
    return [result, { refresh, isLoading, error }];
};

const useCollapseState = (module, defaultActiveKeys) => {
    const dashboardContext = useContext(Contexts.dashboard);
    const [activeKeys, setActiveKeys] = useState(defaultActiveKeys || []);
    useEffect(() => {
        if (module.state.activeCollapseKeys) {
            try {
                const stateKeys = JSON.parse(module.state.activeCollapseKeys);
                setActiveKeys(stateKeys);
            }
            catch {
                // ignored
            }
        }
    }, [module.state.activeCollapseKeys]);
    const handleChange = useCallback((keys) => {
        setActiveKeys(keys);
        const value = JSON.stringify(keys);
        dashboardContext.onStateChanged(module, { activeCollapseKeys: value });
    }, [module, dashboardContext.onStateChanged]);
    return [activeKeys, handleChange];
};

var widgets = {
	volumeControls: {
		title: "Volume",
		volume: "Volume",
		isMuted: "Mute",
		useSystemVolume: "Use System Volume"
	}
};
var defaultEn = {
	widgets: widgets
};

const resources = {
    en: {
        default: defaultEn,
    },
};

export { Contexts, Loader, VolumeControls, WidgetPanel, resources, useApi, useCollapseState, useGetApi };
//# sourceMappingURL=index.es.js.map
