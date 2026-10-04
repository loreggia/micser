import { getCreateModuleMutationOptions, useGetModuleTypes, type ModuleDto, type ModuleTypeDto } from "@micser/web-sdk";
import { useMutation } from "@tanstack/react-query";
import { useReactFlow, type XYPosition } from "@xyflow/react";
import { useNotifyError } from "../notifications";
import { usePluginWidgets } from "../plugins";

/**
 * Returns a function that adds a module of a type, at a graph position (relative to the subgraph if given) or near the center of the
 * visible graph, and resolves to the new module (undefined if adding failed). The module also appears through the engine's change
 * notification.
 */
export function useAddModule() {
  const { screenToFlowPosition } = useReactFlow();
  const notifyError = useNotifyError();
  const create = useMutation({
    ...getCreateModuleMutationOptions(),
    onError: (error) => notifyError("Adding the module failed", error),
  });

  return async (type: string, position?: XYPosition, subgraphId?: string): Promise<ModuleDto | undefined> => {
    if (!position) {
      const pane = document.querySelector(".react-flow")?.getBoundingClientRect();
      position = pane
        ? screenToFlowPosition({ x: pane.left + pane.width / 2 - 120, y: pane.top + pane.height / 3 })
        : { x: 0, y: 0 };
    }

    try {
      return await create.mutateAsync({ data: { type, position, subgraphId } });
    } catch {
      return undefined;
    }
  };
}

/**
 * The module types with their widget titles, sorted by title, for menus.
 */
export function useModuleTypeChoices(): (ModuleTypeDto & { title: string })[] {
  const { data: moduleTypes = [] } = useGetModuleTypes();
  const { widgets } = usePluginWidgets();
  return moduleTypes
    .map((type) => ({ ...type, title: widgets.get(type.type)?.title ?? type.type }))
    .sort((a, b) => a.title.localeCompare(b.title));
}
