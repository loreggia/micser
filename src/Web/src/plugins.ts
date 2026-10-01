import { mainPlugin } from "@micser/plugin-main";
import type { Plugin, WidgetDefinition } from "@micser/web-sdk";

export const plugins: Plugin[] = [mainPlugin];

/**
 * The widgets of all plugins by module type.
 */
export const widgets: ReadonlyMap<string, WidgetDefinition> = new Map(
  plugins.flatMap((plugin) => plugin.widgets).map((widget) => [widget.moduleType, widget])
);
