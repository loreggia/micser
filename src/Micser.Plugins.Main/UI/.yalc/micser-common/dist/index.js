'use strict';

Object.defineProperty(exports, '__esModule', { value: true });

var React = require('react');
var styled = require('styled-components');
var antd = require('antd');
var reactI18next = require('react-i18next');
var AxiosStatic = require('axios');
var lodash = require('lodash');

function _interopDefaultLegacy (e) { return e && typeof e === 'object' && 'default' in e ? e : { 'default': e }; }

var React__default = /*#__PURE__*/_interopDefaultLegacy(React);
var styled__default = /*#__PURE__*/_interopDefaultLegacy(styled);
var AxiosStatic__default = /*#__PURE__*/_interopDefaultLegacy(AxiosStatic);

const Container = styled__default['default'].div `
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
    const [isVisibleInternal, setIsVisibleInternal] = React.useState(false);
    React.useEffect(() => {
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
    return isVisibleInternal ? (React__default['default'].createElement(Container, null,
        React__default['default'].createElement(antd.Spin, { tip: tip }))) : null;
};
Loader.defaultProps = {
    isVisible: true,
    tip: "Loading...",
    suspenseTime: 1000,
};

const Contexts = {
    widgetTypes: React.createContext([]),
    dashboard: React.createContext({
        onStateChanged: () => { },
    }),
};

const parseBool = (value) => {
    return value && value.toLowerCase() === "true";
};

const VolumeControls = ({ module }) => {
    const { t } = reactI18next.useTranslation();
    const dashboardContext = React.useContext(Contexts.dashboard);
    const [volume, setVolume] = React.useState(100);
    const [isMuted, setIsMuted] = React.useState(false);
    const [useSystemVolume, setUseSystemVolume] = React.useState(false);
    React.useEffect(() => {
        const parsed = parseFloat(module.state.volume);
        if (!isNaN(parsed)) {
            setVolume(parsed * 100);
        }
    }, [module.state.volume]);
    React.useEffect(() => {
        const parsed = parseBool(module.state.isMuted);
        setIsMuted(parsed);
    }, [module.state.isMuted]);
    React.useEffect(() => {
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
    return (React__default['default'].createElement(React__default['default'].Fragment, null,
        React__default['default'].createElement(antd.Row, null,
            React__default['default'].createElement(antd.Col, { span: 24 }, t("widgets.volumeControls.volume")),
            React__default['default'].createElement(antd.Col, { span: 16, onMouseDown: stopPropagationHandler, onTouchStart: stopPropagationHandler },
                React__default['default'].createElement(antd.Slider, { min: 0, max: 100, value: volume, onChange: (value) => handleVolumeChange(value, true), onAfterChange: handleVolumeChange, tipFormatter: (value) => `${value}%`, step: 1 })),
            React__default['default'].createElement(antd.Col, { span: 8 },
                React__default['default'].createElement(antd.InputNumber, { min: 0, max: 100, value: volume, onChange: handleVolumeChange, step: 1, style: { float: "right" } }))),
        React__default['default'].createElement(antd.Row, null,
            React__default['default'].createElement(antd.Col, null,
                React__default['default'].createElement(antd.Checkbox, { checked: isMuted, onChange: handleIsMutedChange }, t("widgets.volumeControls.isMuted")),
                React__default['default'].createElement(antd.Checkbox, { checked: useSystemVolume, onChange: handleUseSystemVolumeChange }, t("widgets.volumeControls.useSystemVolume"))))));
};

const WidgetPanel = styled__default['default'](antd.Collapse.Panel) ``;

class Api {
    constructor(baseUrl, onBeginRequest, onEndRequest, onError) {
        this._axios = AxiosStatic__default['default'].create({
            withCredentials: true,
            baseURL: "/api/" + lodash.trimStart(baseUrl, "/"),
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
    const [api, setApi] = React.useState();
    const [isLoading, setIsLoading] = React.useState(false);
    const [error, setError] = React.useState();
    React.useEffect(() => {
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
    const [result, setResult] = React.useState(null);
    const [refreshIndex, setRefreshIndex] = React.useState(0);
    React.useEffect(() => {
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
    const refresh = React.useCallback(() => {
        setRefreshIndex((i) => i + 1);
    }, []);
    return [result, { refresh, isLoading, error }];
};

const useCollapseState = (module, defaultActiveKeys) => {
    const dashboardContext = React.useContext(Contexts.dashboard);
    const [activeKeys, setActiveKeys] = React.useState(defaultActiveKeys || []);
    React.useEffect(() => {
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
    const handleChange = React.useCallback((keys) => {
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

exports.Contexts = Contexts;
exports.Loader = Loader;
exports.VolumeControls = VolumeControls;
exports.WidgetPanel = WidgetPanel;
exports.resources = resources;
exports.useApi = useApi;
exports.useCollapseState = useCollapseState;
exports.useGetApi = useGetApi;
//# sourceMappingURL=index.js.map
