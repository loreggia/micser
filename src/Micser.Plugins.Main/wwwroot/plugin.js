!function(e,t){"object"==typeof exports&&"object"==typeof module?module.exports=t(require("react"),require("antd"),require("micser-common"),require("styled-components")):"function"==typeof define&&define.amd?define("micser-plugins-main",["react","antd","micser-common","styled-components"],t):"object"==typeof exports?exports["micser-plugins-main"]=t(require("react"),require("antd"),require("micser-common"),require("styled-components")):e["micser-plugins-main"]=t(e.react,e.antd,e["micser-common"],e["styled-components"])}(self,(function(e,t,r,n){return(()=>{"use strict";var o={519:(e,t,r)=>{r.r(t),r.d(t,{default:()=>g});var n=r(297),o=r.n(n),i=r(953),a=r(440),u=r(914),c=r.n(u);function l(){var e=f(["\n    width: 100%;\n"]);return l=function(){return e},e}function d(){var e=f(["\n    position: relative;\n    min-width: 200px;\n    max-width: 500px;\n"]);return d=function(){return e},e}function f(e,t){return t||(t=e.slice(0)),Object.freeze(Object.defineProperties(e,{raw:{value:Object.freeze(t)}}))}var s=c().div(d()),m=c()(i.Select)(l());function p(e,t){(null==t||t>e.length)&&(t=e.length);for(var r=0,n=new Array(t);r<t;r++)n[r]=e[r];return n}var y=i.Select.Option;const v=function(e){var t,r,n=e.data,i=(t=(0,a.useApi)("Devices/Input"),r=2,function(e){if(Array.isArray(e))return e}(t)||function(e,t){if("undefined"!=typeof Symbol&&Symbol.iterator in Object(e)){var r=[],n=!0,o=!1,i=void 0;try{for(var a,u=e[Symbol.iterator]();!(n=(a=u.next()).done)&&(r.push(a.value),!t||r.length!==t);n=!0);}catch(e){o=!0,i=e}finally{try{n||null==u.return||u.return()}finally{if(o)throw i}}return r}}(t,r)||function(e,t){if(e){if("string"==typeof e)return p(e,t);var r=Object.prototype.toString.call(e).slice(8,-1);return"Object"===r&&e.constructor&&(r=e.constructor.name),"Map"===r||"Set"===r?Array.from(e):"Arguments"===r||/^(?:Ui|I)nt(?:8|16|32)(?:Clamped)?Array$/.test(r)?p(e,t):void 0}}(t,r)||function(){throw new TypeError("Invalid attempt to destructure non-iterable instance.\nIn order to be iterable, non-array objects must have a [Symbol.iterator]() method.")}()),u=i[0],c=i[1];return o().createElement(s,null,c&&o().createElement(a.Loader,null),o().createElement(m,{defaultValue:n.state.deviceId,dropdownMatchSelectWidth:!1},u&&u.map((function(e){return o().createElement(y,{key:e.id,value:e.id},e.friendlyName)}))))};function b(e,t){(null==t||t>e.length)&&(t=e.length);for(var r=0,n=new Array(t);r<t;r++)n[r]=e[r];return n}var h=i.Select.Option;const S=function(e){var t,r,n=e.data,i=(t=(0,a.useApi)("Devices/Output"),r=2,function(e){if(Array.isArray(e))return e}(t)||function(e,t){if("undefined"!=typeof Symbol&&Symbol.iterator in Object(e)){var r=[],n=!0,o=!1,i=void 0;try{for(var a,u=e[Symbol.iterator]();!(n=(a=u.next()).done)&&(r.push(a.value),!t||r.length!==t);n=!0);}catch(e){o=!0,i=e}finally{try{n||null==u.return||u.return()}finally{if(o)throw i}}return r}}(t,r)||function(e,t){if(e){if("string"==typeof e)return b(e,t);var r=Object.prototype.toString.call(e).slice(8,-1);return"Object"===r&&e.constructor&&(r=e.constructor.name),"Map"===r||"Set"===r?Array.from(e):"Arguments"===r||/^(?:Ui|I)nt(?:8|16|32)(?:Clamped)?Array$/.test(r)?b(e,t):void 0}}(t,r)||function(){throw new TypeError("Invalid attempt to destructure non-iterable instance.\nIn order to be iterable, non-array objects must have a [Symbol.iterator]() method.")}()),u=i[0],c=i[1];return o().createElement(s,null,c&&o().createElement(a.Loader,null),o().createElement(m,{defaultValue:n.state.deviceId,dropdownMatchSelectWidth:!1},u&&u.map((function(e){return o().createElement(h,{key:e.id,value:e.id},e.friendlyName)}))))},g=function(){return{name:"Main",widgets:[{name:"DeviceInput",content:v,outputHandles:["Output01"]},{name:"DeviceOutput",content:S,inputHandles:["Input01"]}]}}},953:e=>{e.exports=t},440:e=>{e.exports=r},297:t=>{t.exports=e},914:e=>{e.exports=n}},i={};function a(e){if(i[e])return i[e].exports;var t=i[e]={exports:{}};return o[e](t,t.exports,a),t.exports}return a.n=e=>{var t=e&&e.__esModule?()=>e.default:()=>e;return a.d(t,{a:t}),t},a.d=(e,t)=>{for(var r in t)a.o(t,r)&&!a.o(e,r)&&Object.defineProperty(e,r,{enumerable:!0,get:t[r]})},a.o=(e,t)=>Object.prototype.hasOwnProperty.call(e,t),a.r=e=>{"undefined"!=typeof Symbol&&Symbol.toStringTag&&Object.defineProperty(e,Symbol.toStringTag,{value:"Module"}),Object.defineProperty(e,"__esModule",{value:!0})},a(519)})()}));
//# sourceMappingURL=plugin.js.map