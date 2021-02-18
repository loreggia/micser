import { FC } from "react";
import { Module } from "./Api";

export interface Widget {
    name: string;
    content: WidgetFC;
    inputHandles?: string[];
    outputHandles?: string[];
}

export interface WidgetProps {
    data: Module;
}

export type WidgetFC = FC<WidgetProps>;
