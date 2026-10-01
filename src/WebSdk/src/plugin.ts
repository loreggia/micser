import type { ComponentType } from "react";
import type { ModuleDto } from "./api";

export type ModuleType = ModuleDto["type"];

export type ModuleOfType<T extends ModuleType> = Extract<ModuleDto, { type: T }>;

/**
 * Props of a widget, the UI of one module type inside its node on the graph.
 */
export interface WidgetProps<T extends ModuleType = ModuleType> {
  module: ModuleOfType<T>;

  /**
   * Replaces the module's state. The change shows immediately and is sent to the engine.
   */
  setState: (state: ModuleOfType<T>["state"]) => void;
}

/**
 * The UI of a module type. Connectors come from the engine's module types, not from here.
 */
export interface WidgetDefinition {
  component: ComponentType<WidgetProps>;
  moduleType: ModuleType;

  /**
   * The display name of the module type, e.g. in the "add module" menu.
   */
  title: string;
}

export function defineWidget<T extends ModuleType>(definition: {
  component: ComponentType<WidgetProps<T>>;
  moduleType: T;
  title: string;
}): WidgetDefinition {
  return definition as unknown as WidgetDefinition;
}

/**
 * The UI half of a plugin: the widgets for the modules the plugin's .NET project provides.
 */
export interface Plugin {
  name: string;
  widgets: WidgetDefinition[];
}
