import { createContext } from "react";
import { StateChangedHandler, Widget } from "./models";
import { Problem } from "./services";

export interface DashboardContext {
    onStateChanged: StateChangedHandler;
}

export type WidgetTypesContext = Widget[];

export interface ErrorContext {
    onError: (error?: Problem) => void;
}

export const Contexts = {
    widgetTypes: createContext<WidgetTypesContext>([]),
    dashboard: createContext<DashboardContext>({
        onStateChanged: () => {
            // nop
        },
    }),
    error: createContext<ErrorContext>({
        onError: () => {
            // nop
        },
    }),
};
