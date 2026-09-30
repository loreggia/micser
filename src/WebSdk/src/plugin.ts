import type { ComponentType } from "react";

/**
 * Props passed to every widget rendered on the routing graph.
 */
export interface WidgetProps {
    moduleId: string;
}

/**
 * Associates a widget component with an engine module type.
 * Connectors are read from the engine's module definition, not declared here.
 */
export interface WidgetDefinition {
    moduleType: string;
    component: ComponentType<WidgetProps>;
}

/**
 * The UI half of a plugin: the widgets for the modules the plugin's .NET project provides.
 */
export interface Plugin {
    name: string;
    widgets: WidgetDefinition[];
}
