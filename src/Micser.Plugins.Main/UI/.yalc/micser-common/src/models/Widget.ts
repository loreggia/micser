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

export type StateChangedHandler = (module: Module, state: ModuleState) => void;

export interface WidgetProps {
    module: Module;
}

export type WidgetFC = FC<WidgetProps>;
