import { FC } from "react";
import { Module } from "./Api";

export interface Widget {
    name: string;
    titleResource: string;
    descriptionResource: string;
    content: WidgetFC;
    inputHandles?: string[];
    outputHandles?: string[];
}

export interface WidgetProps {
    module: Module;
}

export type WidgetFC = FC<WidgetProps>;
