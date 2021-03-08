/// <reference types="react" />
import { StateChangedHandler, Widget } from "./models";
import { Problem } from "./services";
export interface DashboardContext {
    onStateChanged: StateChangedHandler;
}
export declare type WidgetTypesContext = Widget[];
export interface ErrorContext {
    onError: (error?: Problem) => void;
}
export declare const Contexts: {
    widgetTypes: import("react").Context<WidgetTypesContext>;
    dashboard: import("react").Context<DashboardContext>;
    error: import("react").Context<ErrorContext>;
};
