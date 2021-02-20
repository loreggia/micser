import { createContext } from "react";
import { StateChangedHandler, Widget } from "./models";

export interface DashboardContext {
    onStateChanged: StateChangedHandler;
}

export type WidgetTypesContext = Widget[];

export const Contexts = {
    widgetTypes: createContext<WidgetTypesContext>([]),
    dashboard: createContext<DashboardContext>({
        onStateChanged: () => {},
    }),
};
