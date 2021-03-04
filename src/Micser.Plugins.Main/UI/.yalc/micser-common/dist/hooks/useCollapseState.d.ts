import { Module } from "models";
declare type CollapseKeys = string | string[];
export declare const useCollapseState: (module: Module, defaultActiveKeys?: CollapseKeys) => [CollapseKeys, (keys: CollapseKeys) => void];
export {};
