/*
 * ATTENTION: The "eval" devtool has been used (maybe by default in mode: "development").
 * This devtool is not neither made for production nor for readable output files.
 * It uses "eval()" calls to create a separate source file in the browser devtools.
 * If you are trying to read the output file, select a different devtool (https://webpack.js.org/configuration/devtool/)
 * or disable the default devtool with "devtool: false".
 * If you are looking for production-ready output files, see mode: "production" (https://webpack.js.org/configuration/mode/).
 */
(function webpackUniversalModuleDefinition(root, factory) {
	if(typeof exports === 'object' && typeof module === 'object')
		module.exports = factory(require("react"), require("antd"), require("micser-common"), require("styled-components"));
	else if(typeof define === 'function' && define.amd)
		define("micser-plugins-main", ["react", "antd", "micser-common", "styled-components"], factory);
	else if(typeof exports === 'object')
		exports["micser-plugins-main"] = factory(require("react"), require("antd"), require("micser-common"), require("styled-components"));
	else
		root["micser-plugins-main"] = factory(root["react"], root["antd"], root["micser-common"], root["styled-components"]);
})(self, function(__WEBPACK_EXTERNAL_MODULE_react__, __WEBPACK_EXTERNAL_MODULE_antd__, __WEBPACK_EXTERNAL_MODULE_micser_common__, __WEBPACK_EXTERNAL_MODULE_styled_components__) {
return /******/ (() => { // webpackBootstrap
/******/ 	"use strict";
/******/ 	var __webpack_modules__ = ({

/***/ "./src/components/Common.js":
/*!**********************************!*\
  !*** ./src/components/Common.js ***!
  \**********************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

eval("__webpack_require__.r(__webpack_exports__);\n/* harmony export */ __webpack_require__.d(__webpack_exports__, {\n/* harmony export */   \"WidgetContainer\": () => /* binding */ WidgetContainer,\n/* harmony export */   \"DeviceSelect\": () => /* binding */ DeviceSelect\n/* harmony export */ });\n/* harmony import */ var antd__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! antd */ \"antd\");\n/* harmony import */ var antd__WEBPACK_IMPORTED_MODULE_0___default = /*#__PURE__*/__webpack_require__.n(antd__WEBPACK_IMPORTED_MODULE_0__);\n/* harmony import */ var styled_components__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! styled-components */ \"styled-components\");\n/* harmony import */ var styled_components__WEBPACK_IMPORTED_MODULE_1___default = /*#__PURE__*/__webpack_require__.n(styled_components__WEBPACK_IMPORTED_MODULE_1__);\nfunction _templateObject2() {\n  var data = _taggedTemplateLiteral([\"\\n    width: 100%;\\n\"]);\n\n  _templateObject2 = function _templateObject2() {\n    return data;\n  };\n\n  return data;\n}\n\nfunction _templateObject() {\n  var data = _taggedTemplateLiteral([\"\\n    position: relative;\\n    min-width: 200px;\\n    max-width: 500px;\\n\"]);\n\n  _templateObject = function _templateObject() {\n    return data;\n  };\n\n  return data;\n}\n\nfunction _taggedTemplateLiteral(strings, raw) { if (!raw) { raw = strings.slice(0); } return Object.freeze(Object.defineProperties(strings, { raw: { value: Object.freeze(raw) } })); }\n\n\n\nvar WidgetContainer = styled_components__WEBPACK_IMPORTED_MODULE_1___default().div(_templateObject());\nvar DeviceSelect = styled_components__WEBPACK_IMPORTED_MODULE_1___default()(antd__WEBPACK_IMPORTED_MODULE_0__.Select)(_templateObject2());\n\n//# sourceURL=webpack://micser-plugins-main/./src/components/Common.js?");

/***/ }),

/***/ "./src/components/DeviceInputWidget.js":
/*!*********************************************!*\
  !*** ./src/components/DeviceInputWidget.js ***!
  \*********************************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

eval("__webpack_require__.r(__webpack_exports__);\n/* harmony export */ __webpack_require__.d(__webpack_exports__, {\n/* harmony export */   \"default\": () => __WEBPACK_DEFAULT_EXPORT__\n/* harmony export */ });\n/* harmony import */ var react__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! react */ \"react\");\n/* harmony import */ var react__WEBPACK_IMPORTED_MODULE_0___default = /*#__PURE__*/__webpack_require__.n(react__WEBPACK_IMPORTED_MODULE_0__);\n/* harmony import */ var antd__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! antd */ \"antd\");\n/* harmony import */ var antd__WEBPACK_IMPORTED_MODULE_1___default = /*#__PURE__*/__webpack_require__.n(antd__WEBPACK_IMPORTED_MODULE_1__);\n/* harmony import */ var micser_common__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! micser-common */ \"micser-common\");\n/* harmony import */ var micser_common__WEBPACK_IMPORTED_MODULE_2___default = /*#__PURE__*/__webpack_require__.n(micser_common__WEBPACK_IMPORTED_MODULE_2__);\n/* harmony import */ var _Common__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(/*! ./Common */ \"./src/components/Common.js\");\nfunction _slicedToArray(arr, i) { return _arrayWithHoles(arr) || _iterableToArrayLimit(arr, i) || _unsupportedIterableToArray(arr, i) || _nonIterableRest(); }\n\nfunction _nonIterableRest() { throw new TypeError(\"Invalid attempt to destructure non-iterable instance.\\nIn order to be iterable, non-array objects must have a [Symbol.iterator]() method.\"); }\n\nfunction _unsupportedIterableToArray(o, minLen) { if (!o) return; if (typeof o === \"string\") return _arrayLikeToArray(o, minLen); var n = Object.prototype.toString.call(o).slice(8, -1); if (n === \"Object\" && o.constructor) n = o.constructor.name; if (n === \"Map\" || n === \"Set\") return Array.from(o); if (n === \"Arguments\" || /^(?:Ui|I)nt(?:8|16|32)(?:Clamped)?Array$/.test(n)) return _arrayLikeToArray(o, minLen); }\n\nfunction _arrayLikeToArray(arr, len) { if (len == null || len > arr.length) len = arr.length; for (var i = 0, arr2 = new Array(len); i < len; i++) { arr2[i] = arr[i]; } return arr2; }\n\nfunction _iterableToArrayLimit(arr, i) { if (typeof Symbol === \"undefined\" || !(Symbol.iterator in Object(arr))) return; var _arr = []; var _n = true; var _d = false; var _e = undefined; try { for (var _i = arr[Symbol.iterator](), _s; !(_n = (_s = _i.next()).done); _n = true) { _arr.push(_s.value); if (i && _arr.length === i) break; } } catch (err) { _d = true; _e = err; } finally { try { if (!_n && _i[\"return\"] != null) _i[\"return\"](); } finally { if (_d) throw _e; } } return _arr; }\n\nfunction _arrayWithHoles(arr) { if (Array.isArray(arr)) return arr; }\n\n\n\n\n\nvar Option = antd__WEBPACK_IMPORTED_MODULE_1__.Select.Option;\n\nvar DeviceInputWidget = function DeviceInputWidget(_ref) {\n  var data = _ref.data;\n\n  var _useApi = (0,micser_common__WEBPACK_IMPORTED_MODULE_2__.useApi)(\"Devices/Input\"),\n      _useApi2 = _slicedToArray(_useApi, 2),\n      devices = _useApi2[0],\n      isLoading = _useApi2[1];\n\n  return /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0___default().createElement(_Common__WEBPACK_IMPORTED_MODULE_3__.WidgetContainer, null, isLoading && /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0___default().createElement(micser_common__WEBPACK_IMPORTED_MODULE_2__.Loader, null), /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0___default().createElement(_Common__WEBPACK_IMPORTED_MODULE_3__.DeviceSelect, {\n    defaultValue: data.state.deviceId,\n    dropdownMatchSelectWidth: false\n  }, devices && devices.map(function (device) {\n    return /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0___default().createElement(Option, {\n      key: device.id,\n      value: device.id\n    }, device.friendlyName);\n  })));\n};\n\n/* harmony default export */ const __WEBPACK_DEFAULT_EXPORT__ = (DeviceInputWidget);\n\n//# sourceURL=webpack://micser-plugins-main/./src/components/DeviceInputWidget.js?");

/***/ }),

/***/ "./src/components/DeviceOutputWidget.js":
/*!**********************************************!*\
  !*** ./src/components/DeviceOutputWidget.js ***!
  \**********************************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

eval("__webpack_require__.r(__webpack_exports__);\n/* harmony export */ __webpack_require__.d(__webpack_exports__, {\n/* harmony export */   \"default\": () => __WEBPACK_DEFAULT_EXPORT__\n/* harmony export */ });\n/* harmony import */ var react__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! react */ \"react\");\n/* harmony import */ var react__WEBPACK_IMPORTED_MODULE_0___default = /*#__PURE__*/__webpack_require__.n(react__WEBPACK_IMPORTED_MODULE_0__);\n/* harmony import */ var antd__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! antd */ \"antd\");\n/* harmony import */ var antd__WEBPACK_IMPORTED_MODULE_1___default = /*#__PURE__*/__webpack_require__.n(antd__WEBPACK_IMPORTED_MODULE_1__);\n/* harmony import */ var micser_common__WEBPACK_IMPORTED_MODULE_2__ = __webpack_require__(/*! micser-common */ \"micser-common\");\n/* harmony import */ var micser_common__WEBPACK_IMPORTED_MODULE_2___default = /*#__PURE__*/__webpack_require__.n(micser_common__WEBPACK_IMPORTED_MODULE_2__);\n/* harmony import */ var _Common__WEBPACK_IMPORTED_MODULE_3__ = __webpack_require__(/*! ./Common */ \"./src/components/Common.js\");\nfunction _slicedToArray(arr, i) { return _arrayWithHoles(arr) || _iterableToArrayLimit(arr, i) || _unsupportedIterableToArray(arr, i) || _nonIterableRest(); }\n\nfunction _nonIterableRest() { throw new TypeError(\"Invalid attempt to destructure non-iterable instance.\\nIn order to be iterable, non-array objects must have a [Symbol.iterator]() method.\"); }\n\nfunction _unsupportedIterableToArray(o, minLen) { if (!o) return; if (typeof o === \"string\") return _arrayLikeToArray(o, minLen); var n = Object.prototype.toString.call(o).slice(8, -1); if (n === \"Object\" && o.constructor) n = o.constructor.name; if (n === \"Map\" || n === \"Set\") return Array.from(o); if (n === \"Arguments\" || /^(?:Ui|I)nt(?:8|16|32)(?:Clamped)?Array$/.test(n)) return _arrayLikeToArray(o, minLen); }\n\nfunction _arrayLikeToArray(arr, len) { if (len == null || len > arr.length) len = arr.length; for (var i = 0, arr2 = new Array(len); i < len; i++) { arr2[i] = arr[i]; } return arr2; }\n\nfunction _iterableToArrayLimit(arr, i) { if (typeof Symbol === \"undefined\" || !(Symbol.iterator in Object(arr))) return; var _arr = []; var _n = true; var _d = false; var _e = undefined; try { for (var _i = arr[Symbol.iterator](), _s; !(_n = (_s = _i.next()).done); _n = true) { _arr.push(_s.value); if (i && _arr.length === i) break; } } catch (err) { _d = true; _e = err; } finally { try { if (!_n && _i[\"return\"] != null) _i[\"return\"](); } finally { if (_d) throw _e; } } return _arr; }\n\nfunction _arrayWithHoles(arr) { if (Array.isArray(arr)) return arr; }\n\n\n\n\n\nvar Option = antd__WEBPACK_IMPORTED_MODULE_1__.Select.Option;\n\nvar DeviceOutputWidget = function DeviceOutputWidget(_ref) {\n  var data = _ref.data;\n\n  var _useApi = (0,micser_common__WEBPACK_IMPORTED_MODULE_2__.useApi)(\"Devices/Output\"),\n      _useApi2 = _slicedToArray(_useApi, 2),\n      devices = _useApi2[0],\n      isLoading = _useApi2[1];\n\n  return /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0___default().createElement(_Common__WEBPACK_IMPORTED_MODULE_3__.WidgetContainer, null, isLoading && /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0___default().createElement(micser_common__WEBPACK_IMPORTED_MODULE_2__.Loader, null), /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0___default().createElement(_Common__WEBPACK_IMPORTED_MODULE_3__.DeviceSelect, {\n    defaultValue: data.state.deviceId,\n    dropdownMatchSelectWidth: false\n  }, devices && devices.map(function (device) {\n    return /*#__PURE__*/react__WEBPACK_IMPORTED_MODULE_0___default().createElement(Option, {\n      key: device.id,\n      value: device.id\n    }, device.friendlyName);\n  })));\n};\n\n/* harmony default export */ const __WEBPACK_DEFAULT_EXPORT__ = (DeviceOutputWidget);\n\n//# sourceURL=webpack://micser-plugins-main/./src/components/DeviceOutputWidget.js?");

/***/ }),

/***/ "./src/components/index.js":
/*!*********************************!*\
  !*** ./src/components/index.js ***!
  \*********************************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

eval("__webpack_require__.r(__webpack_exports__);\n/* harmony export */ __webpack_require__.d(__webpack_exports__, {\n/* harmony export */   \"DeviceInputWidget\": () => /* reexport safe */ _DeviceInputWidget__WEBPACK_IMPORTED_MODULE_0__.default,\n/* harmony export */   \"DeviceOutputWidget\": () => /* reexport safe */ _DeviceOutputWidget__WEBPACK_IMPORTED_MODULE_1__.default\n/* harmony export */ });\n/* harmony import */ var _DeviceInputWidget__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./DeviceInputWidget */ \"./src/components/DeviceInputWidget.js\");\n/* harmony import */ var _DeviceOutputWidget__WEBPACK_IMPORTED_MODULE_1__ = __webpack_require__(/*! ./DeviceOutputWidget */ \"./src/components/DeviceOutputWidget.js\");\n\n\n\n//# sourceURL=webpack://micser-plugins-main/./src/components/index.js?");

/***/ }),

/***/ "./src/index.js":
/*!**********************!*\
  !*** ./src/index.js ***!
  \**********************/
/***/ ((__unused_webpack_module, __webpack_exports__, __webpack_require__) => {

eval("__webpack_require__.r(__webpack_exports__);\n/* harmony export */ __webpack_require__.d(__webpack_exports__, {\n/* harmony export */   \"default\": () => __WEBPACK_DEFAULT_EXPORT__\n/* harmony export */ });\n/* harmony import */ var _components__WEBPACK_IMPORTED_MODULE_0__ = __webpack_require__(/*! ./components */ \"./src/components/index.js\");\n\n\nvar Plugin = function Plugin() {\n  return {\n    name: \"Main\",\n    widgets: [{\n      name: \"DeviceInput\",\n      content: _components__WEBPACK_IMPORTED_MODULE_0__.DeviceInputWidget,\n      outputHandles: [\"Output01\"]\n    }, {\n      name: \"DeviceOutput\",\n      content: _components__WEBPACK_IMPORTED_MODULE_0__.DeviceOutputWidget,\n      inputHandles: [\"Input01\"]\n    }]\n  };\n};\n\n/* harmony default export */ const __WEBPACK_DEFAULT_EXPORT__ = (Plugin);\n\n//# sourceURL=webpack://micser-plugins-main/./src/index.js?");

/***/ }),

/***/ "antd":
/*!***********************!*\
  !*** external "antd" ***!
  \***********************/
/***/ ((module) => {

eval("module.exports = __WEBPACK_EXTERNAL_MODULE_antd__;\n\n//# sourceURL=webpack://micser-plugins-main/external_%22antd%22?");

/***/ }),

/***/ "micser-common":
/*!********************************!*\
  !*** external "micser-common" ***!
  \********************************/
/***/ ((module) => {

eval("module.exports = __WEBPACK_EXTERNAL_MODULE_micser_common__;\n\n//# sourceURL=webpack://micser-plugins-main/external_%22micser-common%22?");

/***/ }),

/***/ "react":
/*!************************!*\
  !*** external "react" ***!
  \************************/
/***/ ((module) => {

eval("module.exports = __WEBPACK_EXTERNAL_MODULE_react__;\n\n//# sourceURL=webpack://micser-plugins-main/external_%22react%22?");

/***/ }),

/***/ "styled-components":
/*!************************************!*\
  !*** external "styled-components" ***!
  \************************************/
/***/ ((module) => {

eval("module.exports = __WEBPACK_EXTERNAL_MODULE_styled_components__;\n\n//# sourceURL=webpack://micser-plugins-main/external_%22styled-components%22?");

/***/ })

/******/ 	});
/************************************************************************/
/******/ 	// The module cache
/******/ 	var __webpack_module_cache__ = {};
/******/ 	
/******/ 	// The require function
/******/ 	function __webpack_require__(moduleId) {
/******/ 		// Check if module is in cache
/******/ 		if(__webpack_module_cache__[moduleId]) {
/******/ 			return __webpack_module_cache__[moduleId].exports;
/******/ 		}
/******/ 		// Create a new module (and put it into the cache)
/******/ 		var module = __webpack_module_cache__[moduleId] = {
/******/ 			// no module.id needed
/******/ 			// no module.loaded needed
/******/ 			exports: {}
/******/ 		};
/******/ 	
/******/ 		// Execute the module function
/******/ 		__webpack_modules__[moduleId](module, module.exports, __webpack_require__);
/******/ 	
/******/ 		// Return the exports of the module
/******/ 		return module.exports;
/******/ 	}
/******/ 	
/************************************************************************/
/******/ 	/* webpack/runtime/compat get default export */
/******/ 	(() => {
/******/ 		// getDefaultExport function for compatibility with non-harmony modules
/******/ 		__webpack_require__.n = (module) => {
/******/ 			var getter = module && module.__esModule ?
/******/ 				() => module['default'] :
/******/ 				() => module;
/******/ 			__webpack_require__.d(getter, { a: getter });
/******/ 			return getter;
/******/ 		};
/******/ 	})();
/******/ 	
/******/ 	/* webpack/runtime/define property getters */
/******/ 	(() => {
/******/ 		// define getter functions for harmony exports
/******/ 		__webpack_require__.d = (exports, definition) => {
/******/ 			for(var key in definition) {
/******/ 				if(__webpack_require__.o(definition, key) && !__webpack_require__.o(exports, key)) {
/******/ 					Object.defineProperty(exports, key, { enumerable: true, get: definition[key] });
/******/ 				}
/******/ 			}
/******/ 		};
/******/ 	})();
/******/ 	
/******/ 	/* webpack/runtime/hasOwnProperty shorthand */
/******/ 	(() => {
/******/ 		__webpack_require__.o = (obj, prop) => Object.prototype.hasOwnProperty.call(obj, prop)
/******/ 	})();
/******/ 	
/******/ 	/* webpack/runtime/make namespace object */
/******/ 	(() => {
/******/ 		// define __esModule on exports
/******/ 		__webpack_require__.r = (exports) => {
/******/ 			if(typeof Symbol !== 'undefined' && Symbol.toStringTag) {
/******/ 				Object.defineProperty(exports, Symbol.toStringTag, { value: 'Module' });
/******/ 			}
/******/ 			Object.defineProperty(exports, '__esModule', { value: true });
/******/ 		};
/******/ 	})();
/******/ 	
/************************************************************************/
/******/ 	// module exports must be returned from runtime so entry inlining is disabled
/******/ 	// startup
/******/ 	// Load entry module and return exports
/******/ 	return __webpack_require__("./src/index.js");
/******/ })()
;
});