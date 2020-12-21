!function(e,t){"object"==typeof exports&&"object"==typeof module?module.exports=t():"function"==typeof define&&define.amd?define("micser-common",[],t):"object"==typeof exports?exports["micser-common"]=t():e["micser-common"]=t()}(self,(function(){return(()=>{"use strict";var e={374:(e,t,r)=>{r.r(t),r.d(t,{Loader:()=>l,useApi:()=>h});const n=require("react");var o=r.n(n);const a=require("styled-components");var u=r.n(a);const i=require("antd");function c(){var e,t,r=(e=["\n    position: absolute;\n    width: 100%;\n    height: 100%;\n    display: flex;\n    justify-content: center;\n    align-items: center;\n    background-color: rgba(0, 0, 0, 0.5);\n    z-index: 999;\n"],t||(t=e.slice(0)),Object.freeze(Object.defineProperties(e,{raw:{value:Object.freeze(t)}})));return c=function(){return r},r}var s=u().div(c()),f=function(e){var t=e.tip;return o().createElement(s,null,o().createElement(i.Spin,{tip:t}))};f.defaultProps={tip:"Loading..."};const l=f,p=require("axios");var d=r.n(p);function m(e,t,r,n,o,a,u){try{var i=e[a](u),c=i.value}catch(e){return void r(e)}i.done?t(c):Promise.resolve(c).then(n,o)}function v(e){return function(){var t=this,r=arguments;return new Promise((function(n,o){var a=e.apply(t,r);function u(e){m(a,n,o,u,i,"next",e)}function i(e){m(a,n,o,u,i,"throw",e)}u(void 0)}))}}function b(e,t){return function(e){if(Array.isArray(e))return e}(e)||function(e,t){if("undefined"!=typeof Symbol&&Symbol.iterator in Object(e)){var r=[],n=!0,o=!1,a=void 0;try{for(var u,i=e[Symbol.iterator]();!(n=(u=i.next()).done)&&(r.push(u.value),!t||r.length!==t);n=!0);}catch(e){o=!0,a=e}finally{try{n||null==i.return||i.return()}finally{if(o)throw a}}return r}}(e,t)||function(e,t){if(e){if("string"==typeof e)return y(e,t);var r=Object.prototype.toString.call(e).slice(8,-1);return"Object"===r&&e.constructor&&(r=e.constructor.name),"Map"===r||"Set"===r?Array.from(e):"Arguments"===r||/^(?:Ui|I)nt(?:8|16|32)(?:Clamped)?Array$/.test(r)?y(e,t):void 0}}(e,t)||function(){throw new TypeError("Invalid attempt to destructure non-iterable instance.\nIn order to be iterable, non-array objects must have a [Symbol.iterator]() method.")}()}function y(e,t){(null==t||t>e.length)&&(t=e.length);for(var r=0,n=new Array(t);r<t;r++)n[r]=e[r];return n}const h=function(e){var t=arguments.length>1&&void 0!==arguments[1]?arguments[1]:{autoLoad:!0,method:"get"},r=t.autoLoad,o=t.method,a=t.data,u=(0,n.useMemo)((function(){return d().create({baseURL:"/api"})}),[]),i=(0,n.useState)(),c=b(i,2),s=c[0],f=c[1],l=(0,n.useState)(!1),p=b(l,2),m=p[0],y=p[1],h=(0,n.useState)(!1),g=b(h,2),x=g[0],S=g[1],j=(0,n.useState)(),w=b(j,2),O=w[0],k=w[1],P=(0,n.useState)(0),A=b(P,2),E=A[0],L=A[1],M=function(){L(E+1)};return(0,n.useEffect)((function(){var t=!1;return(r||E>1)&&function(){var r=v(regeneratorRuntime.mark((function r(){var n;return regeneratorRuntime.wrap((function(r){for(;;)switch(r.prev=r.next){case 0:y(!0),S(!1),r.prev=2,n=null,r.t0=o,r.next="get"===r.t0?7:"post"===r.t0?11:"put"===r.t0?15:"delete"===r.t0?19:23;break;case 7:return r.next=9,u.get(e,{params:a});case 9:return n=r.sent,r.abrupt("break",24);case 11:return r.next=13,u.post(e,a);case 13:return n=r.sent,r.abrupt("break",24);case 15:return r.next=17,u.put(e,a);case 17:return n=r.sent,r.abrupt("break",24);case 19:return r.next=21,u.delete(e,{params:a});case 21:return n=r.sent,r.abrupt("break",24);case 23:throw new Error("Invalid method");case 24:t||(f(n.data),k(null),y(!1),S(!0)),r.next=31;break;case 27:r.prev=27,r.t1=r.catch(2),console.log(r.t1),t||k(r.t1.response&&r.t1.response.data?r.t1.response.data:{statusCode:500,message:"Unknown error."});case 31:return r.prev=31,t||y(!1),r.finish(31);case 34:case"end":return r.stop()}}),r,null,[[2,27,31,34]])})));return function(){return r.apply(this,arguments)}}()(),function(){t=!0}}),[u,e,r,o,a,E]),[s,m,M,x,O]}}},t={};function r(n){if(t[n])return t[n].exports;var o=t[n]={exports:{}};return e[n](o,o.exports,r),o.exports}return r.n=e=>{var t=e&&e.__esModule?()=>e.default:()=>e;return r.d(t,{a:t}),t},r.d=(e,t)=>{for(var n in t)r.o(t,n)&&!r.o(e,n)&&Object.defineProperty(e,n,{enumerable:!0,get:t[n]})},r.o=(e,t)=>Object.prototype.hasOwnProperty.call(e,t),r.r=e=>{"undefined"!=typeof Symbol&&Symbol.toStringTag&&Object.defineProperty(e,Symbol.toStringTag,{value:"Module"}),Object.defineProperty(e,"__esModule",{value:!0})},r(374)})()}));
//# sourceMappingURL=main.js.map