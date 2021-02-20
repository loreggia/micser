/// <reference types="react" />
import { StateChangedHandler, Widget } from "./models";
export interface DashboardContext {
    onStateChanged: StateChangedHandler;
}
export declare type WidgetTypesContext = Widget[];
export declare const Contexts: {
    widgetTypes: import("react").Context<WidgetTypesContext>;
    dashboard: import("react").Context<DashboardContext>;
};
