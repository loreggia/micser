import {
  getCreateModuleMutationOptions,
  getInstantiateSubgraphTemplateMutationOptions,
  useGetModuleTypes,
  type ModuleDto,
  type ModuleTypeDto,
} from "@micser/web-sdk";
import { useMutation } from "@tanstack/react-query";
import { useReactFlow, type XYPosition } from "@xyflow/react";
import { useNotifyError } from "../notifications";
import { usePluginWidgets } from "../plugins";

/**
 * Returns a function that gives a graph position near the center of the visible graph, for things added without a position.
 */
function useVisibleCenter() {
  const { screenToFlowPosition } = useReactFlow();

  return (): XYPosition => {
    const pane = document.querySelector(".react-flow")?.getBoundingClientRect();
    return pane
      ? screenToFlowPosition({ x: pane.left + pane.width / 2 - 120, y: pane.top + pane.height / 3 })
      : { x: 0, y: 0 };
  };
}

/**
 * Returns a function that adds a module of a type, at a graph position (relative to the subgraph if given) or near the center of the
 * visible graph, and resolves to the new module (undefined if adding failed). The module also appears through the engine's change
 * notification.
 */
export function useAddModule() {
  const visibleCenter = useVisibleCenter();
  const notifyError = useNotifyError();
  const create = useMutation({
    ...getCreateModuleMutationOptions(),
    onError: (error) => notifyError("Adding the module failed", error),
  });

  return async (type: string, position?: XYPosition, subgraphId?: string): Promise<ModuleDto | undefined> => {
    try {
      return await create.mutateAsync({ data: { type, position: position ?? visibleCenter(), subgraphId } });
    } catch {
      return undefined;
    }
  };
}

/**
 * Returns a function that creates a subgraph from a template, at a graph position or near the center of the visible graph. The subgraph
 * appears through the engine's change notifications.
 */
export function useInstantiateTemplate() {
  const visibleCenter = useVisibleCenter();
  const notifyError = useNotifyError();
  const { mutate } = useMutation({
    ...getInstantiateSubgraphTemplateMutationOptions(),
    onError: (error) => notifyError("Adding the subgraph failed", error),
  });

  return (templateId: string, position?: XYPosition) =>
    mutate({ id: templateId, data: { position: position ?? visibleCenter() } });
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
