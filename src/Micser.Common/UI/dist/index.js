'use strict';

Object.defineProperty(exports, '__esModule', { value: true });

var React = require('react');
var styled = require('styled-components');
var antd = require('antd');
var AxiosStatic = require('axios');
var lodash = require('lodash');

function _interopDefaultLegacy (e) { return e && typeof e === 'object' && 'default' in e ? e : { 'default': e }; }

var React__default = /*#__PURE__*/_interopDefaultLegacy(React);
var styled__default = /*#__PURE__*/_interopDefaultLegacy(styled);
var AxiosStatic__default = /*#__PURE__*/_interopDefaultLegacy(AxiosStatic);

/*! *****************************************************************************
Copyright (c) Microsoft Corporation.

Permission to use, copy, modify, and/or distribute this software for any
purpose with or without fee is hereby granted.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
PERFORMANCE OF THIS SOFTWARE.
***************************************************************************** */

function __awaiter(thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
}

function __generator(thisArg, body) {
    var _ = { label: 0, sent: function() { if (t[0] & 1) throw t[1]; return t[1]; }, trys: [], ops: [] }, f, y, t, g;
    return g = { next: verb(0), "throw": verb(1), "return": verb(2) }, typeof Symbol === "function" && (g[Symbol.iterator] = function() { return this; }), g;
    function verb(n) { return function (v) { return step([n, v]); }; }
    function step(op) {
        if (f) throw new TypeError("Generator is already executing.");
        while (_) try {
            if (f = 1, y && (t = op[0] & 2 ? y["return"] : op[0] ? y["throw"] || ((t = y["return"]) && t.call(y), 0) : y.next) && !(t = t.call(y, op[1])).done) return t;
            if (y = 0, t) op = [op[0] & 2, t.value];
            switch (op[0]) {
                case 0: case 1: t = op; break;
                case 4: _.label++; return { value: op[1], done: false };
                case 5: _.label++; y = op[1]; op = [0]; continue;
                case 7: op = _.ops.pop(); _.trys.pop(); continue;
                default:
                    if (!(t = _.trys, t = t.length > 0 && t[t.length - 1]) && (op[0] === 6 || op[0] === 2)) { _ = 0; continue; }
                    if (op[0] === 3 && (!t || (op[1] > t[0] && op[1] < t[3]))) { _.label = op[1]; break; }
                    if (op[0] === 6 && _.label < t[1]) { _.label = t[1]; t = op; break; }
                    if (t && _.label < t[2]) { _.label = t[2]; _.ops.push(op); break; }
                    if (t[2]) _.ops.pop();
                    _.trys.pop(); continue;
            }
            op = body.call(thisArg, _);
        } catch (e) { op = [6, e]; y = 0; } finally { f = t = 0; }
        if (op[0] & 5) throw op[1]; return { value: op[0] ? op[1] : void 0, done: true };
    }
}

function __makeTemplateObject(cooked, raw) {
    if (Object.defineProperty) { Object.defineProperty(cooked, "raw", { value: raw }); } else { cooked.raw = raw; }
    return cooked;
}

var Container = styled__default['default'].div(templateObject_1 || (templateObject_1 = __makeTemplateObject(["\n    position: absolute;\n    width: 100%;\n    height: 100%;\n    display: flex;\n    justify-content: center;\n    align-items: center;\n    background-color: rgba(0, 0, 0, 0.5);\n    z-index: 999;\n"], ["\n    position: absolute;\n    width: 100%;\n    height: 100%;\n    display: flex;\n    justify-content: center;\n    align-items: center;\n    background-color: rgba(0, 0, 0, 0.5);\n    z-index: 999;\n"])));
var Loader = function (_a) {
    var isVisible = _a.isVisible, tip = _a.tip, suspenseTime = _a.suspenseTime;
    var _b = React.useState(false), isVisibleInternal = _b[0], setIsVisibleInternal = _b[1];
    React.useEffect(function () {
        var timeout;
        if (isVisible) {
            var handler = function () {
                setIsVisibleInternal(true);
            };
            timeout = setTimeout(handler, suspenseTime);
        }
        else {
            clearTimeout(timeout);
            setIsVisibleInternal(false);
        }
        return function () {
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
var templateObject_1;

var Contexts = {
    widgetTypes: React.createContext([]),
    dashboard: React.createContext({
        onStateChanged: function () { },
    }),
};

var parseBool = function (value) {
    return value && value.toLowerCase() === "true";
};

var CommonControls = function (_a) {
    var module = _a.module;
    var dashboardContext = React.useContext(Contexts.dashboard);
    var _b = React.useState(100), volume = _b[0], setVolume = _b[1];
    var _c = React.useState(false), isMuted = _c[0], setIsMuted = _c[1];
    var _d = React.useState(false), useSystemVolume = _d[0], setUseSystemVolume = _d[1];
    React.useEffect(function () {
        var parsed = parseInt(module.state.volume);
        if (!isNaN(parsed)) {
            setVolume(parsed);
        }
    }, [module.state.volume]);
    React.useEffect(function () {
        var parsed = parseBool(module.state.isMuted);
        setIsMuted(parsed);
    }, [module.state.isMuted]);
    React.useEffect(function () {
        var parsed = parseBool(module.state.useSystemVolume);
        setUseSystemVolume(parsed);
    }, [module.state.useSystemVolume]);
    var handleVolumeChange = function (value) {
        setVolume(value);
        dashboardContext === null || dashboardContext === void 0 ? void 0 : dashboardContext.onStateChanged(module, { volume: value.toString() });
    };
    var handleIsMutedChange = function (e) {
        var value = e.target.checked;
        setIsMuted(value);
        dashboardContext === null || dashboardContext === void 0 ? void 0 : dashboardContext.onStateChanged(module, { isMuted: value.toString() });
    };
    var handleUseSystemVolumeChange = function (e) {
        var value = e.target.checked;
        setUseSystemVolume(e.target.checked);
        dashboardContext === null || dashboardContext === void 0 ? void 0 : dashboardContext.onStateChanged(module, { useSystemVolume: value.toString() });
    };
    return (React__default['default'].createElement(antd.Collapse, { defaultActiveKey: ["common-controls"] },
        React__default['default'].createElement(antd.Collapse.Panel, { header: "Common Controls", key: "common-controls" },
            React__default['default'].createElement(antd.Slider, { min: 0, max: 100, value: volume, onChange: handleVolumeChange }),
            React__default['default'].createElement(antd.Checkbox, { checked: isMuted, onChange: handleIsMutedChange }, "Mute"),
            React__default['default'].createElement(antd.Checkbox, { checked: useSystemVolume, onChange: handleUseSystemVolumeChange }, "Use system volume"))));
};

var Api = /** @class */ (function () {
    function Api(baseUrl, onBeginRequest, onEndRequest, onError) {
        this._axios = AxiosStatic__default['default'].create({
            withCredentials: true,
            baseURL: "/api/" + lodash.trimStart(baseUrl, "/"),
        });
        this._onBeginRequest = onBeginRequest;
        this._onEndRequest = onEndRequest;
        this._onError = onError;
    }
    Api.prototype.get = function (action, params) {
        if (action === void 0) { action = ""; }
        return __awaiter(this, void 0, void 0, function () {
            var _this = this;
            return __generator(this, function (_a) {
                return [2 /*return*/, this.execute(function () { return _this._axios.get(action, { params: params }); })];
            });
        });
    };
    Api.prototype.getList = function (action, params) {
        if (action === void 0) { action = ""; }
        return __awaiter(this, void 0, void 0, function () {
            var _this = this;
            return __generator(this, function (_a) {
                return [2 /*return*/, this.execute(function () { return _this._axios.get(action, { params: params }); })];
            });
        });
    };
    Api.prototype.post = function (action, data) {
        if (action === void 0) { action = ""; }
        return __awaiter(this, void 0, void 0, function () {
            var _this = this;
            return __generator(this, function (_a) {
                return [2 /*return*/, this.execute(function () { return _this._axios.post(action, data); })];
            });
        });
    };
    Api.prototype.put = function (action, data) {
        return __awaiter(this, void 0, void 0, function () {
            var _this = this;
            return __generator(this, function (_a) {
                return [2 /*return*/, this.execute(function () { return _this._axios.put("" + action, data); })];
            });
        });
    };
    Api.prototype.delete = function (action, params) {
        return __awaiter(this, void 0, void 0, function () {
            var _this = this;
            return __generator(this, function (_a) {
                return [2 /*return*/, this.execute(function () { return _this._axios.delete("" + action, { params: params }); })];
            });
        });
    };
    Api.prototype.execute = function (func) {
        return __awaiter(this, void 0, void 0, function () {
            var result, error_1;
            return __generator(this, function (_a) {
                switch (_a.label) {
                    case 0:
                        _a.trys.push([0, 2, 3, 4]);
                        this._onBeginRequest();
                        return [4 /*yield*/, func()];
                    case 1:
                        result = _a.sent();
                        return [2 /*return*/, this.getResult(result)];
                    case 2:
                        error_1 = _a.sent();
                        return [2 /*return*/, this.handleError(error_1)];
                    case 3:
                        this._onEndRequest();
                        return [7 /*endfinally*/];
                    case 4: return [2 /*return*/];
                }
            });
        });
    };
    Api.prototype.getResult = function (axiosResult) {
        var isSuccess = axiosResult.status >= 200 && axiosResult.status < 400;
        return {
            isSuccess: isSuccess,
            problem: isSuccess ? undefined : axiosResult.data,
            data: isSuccess ? axiosResult.data : undefined,
        };
    };
    Api.prototype.handleError = function (error) {
        console.log(error);
        var problem = error.response && error.response.data
            ? error.response.data
            : {
                type: "Internal",
                status: 500,
                title: "Unknown error.",
            };
        this._onError(problem);
        return {
            isSuccess: false,
            problem: problem,
        };
    };
    return Api;
}());

function useApi(path) {
    var _a = React.useState(null), api = _a[0], setApi = _a[1];
    var _b = React.useState(false), isLoading = _b[0], setIsLoading = _b[1];
    var _c = React.useState(), error = _c[0], setError = _c[1];
    React.useEffect(function () {
        var canceled = false;
        var onError = function (error) {
            if (!canceled) {
                setError(error);
            }
        };
        var onRequestAction = function (isLoading) {
            if (!canceled) {
                setIsLoading(isLoading);
            }
        };
        var api = new Api(path, function () { return onRequestAction(true); }, function () { return onRequestAction(false); }, onError);
        setApi(api);
        return function () {
            canceled = true;
        };
    }, []);
    return [api, { isLoading: isLoading, error: error }];
}

var useGetApi = function (path, action, params) {
    var _a = useApi(path), api = _a[0], _b = _a[1], isLoading = _b.isLoading, error = _b.error;
    var _c = React.useState(null), result = _c[0], setResult = _c[1];
    var _d = React.useState(0), refreshIndex = _d[0], setRefreshIndex = _d[1];
    React.useEffect(function () {
        var canceled = false;
        var loadData = function () { return __awaiter(void 0, void 0, void 0, function () {
            var result_1;
            return __generator(this, function (_a) {
                switch (_a.label) {
                    case 0:
                        if (!api) return [3 /*break*/, 2];
                        return [4 /*yield*/, api.get(action || "", params)];
                    case 1:
                        result_1 = _a.sent();
                        if (!canceled && result_1.isSuccess) {
                            setResult(result_1.data || null);
                        }
                        _a.label = 2;
                    case 2: return [2 /*return*/];
                }
            });
        }); };
        loadData();
        return function () {
            canceled = true;
        };
    }, [api, params, refreshIndex]);
    var refresh = React.useCallback(function () {
        setRefreshIndex(function (i) { return i + 1; });
    }, []);
    return [result, { refresh: refresh, isLoading: isLoading, error: error }];
};

exports.CommonControls = CommonControls;
exports.Contexts = Contexts;
exports.Loader = Loader;
exports.useApi = useApi;
exports.useGetApi = useGetApi;
//# sourceMappingURL=index.js.map
