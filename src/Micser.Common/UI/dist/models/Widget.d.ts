import { FC } from "react";
import { Module, ModuleState } from "./Api";
export interface Widget {
    name: string;
    titleResource: string;
    descriptionResource: string;
    content: WidgetFC;
    inputHandles?: string[];
    outputHandles?: string[];
}
export declare type StateChangedHandler = (module: Module, state: Partial<ModuleState>) => void;
export interface WidgetProps {
    module: Module;
}
export declare type WidgetFC = FC<WidgetProps>;
