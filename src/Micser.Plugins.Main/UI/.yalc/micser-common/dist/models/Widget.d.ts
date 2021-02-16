import { FC } from "react";
import { Module } from "./Api";
export interface WidgetContent {
    (props: {
        data: Module;
    }): JSX.Element;
}
export interface WidgetType {
    name: string;
    content: WidgetContent;
    inputHandles?: string[];
    outputHandles?: string[];
}
export interface WidgetProps {
    data: Module;
}
export declare type WidgetFC = FC<WidgetProps>;
