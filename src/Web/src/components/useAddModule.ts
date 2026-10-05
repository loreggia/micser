import {
  getCreateModuleMutationOptions,
  getInstantiateSubgraphTemplateMutationOptions,
  localize,
  useGetModuleTypes,
  useLanguage,
  type ModuleDto,
  type ModuleTypeDto,
} from "@micser/web-sdk";
import { useMutation } from "@tanstack/react-query";
import { useReactFlow, type XYPosition } from "@xyflow/react";
import { useTranslation } from "../i18n";
import { useNotifyError } from "../notifications";
import { usePluginWidgets } from "../plugins";
import type { ModuleNodeType } from "./ModuleNode";
import { pendingPlacement, pendingSelection } from "./newModules";
import { findFreePosition, placementMinimum, placementObstacles } from "./placement";

/** The size assumed for a new module of a type that isn't on the graph yet. */
const defaultModuleSize = { width: 260, height: 140 };

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
 * Returns a function that moves a position for a new module of a type to the nearest one where it doesn't overlap other nodes. In a
 * subgraph, the position is relative to it, only the subgraph's modules count, and the module stays below the frame's header; otherwise
 * the modules and subgraphs outside of subgraphs count. The new module's size is taken from a module of the same type on the graph, preferably an expanded one.
 */
function useFreeModulePosition() {
  const { getNodes } = useReactFlow();

  return (type: string, wanted: XYPosition, subgraphId?: string): XYPosition => {
    const nodes = getNodes();
    const sameType = nodes.filter(
      (node) => node.type === "module" && (node as ModuleNodeType).data.module.type === type && node.measured?.width
    ) as ModuleNodeType[];
    const sample = sameType.find((node) => !node.data.module.isCollapsed) ?? sameType[0];
    const size =
      sample?.measured?.width && sample.measured.height
        ? { width: sample.measured.width, height: sample.measured.height }
        : defaultModuleSize;

    return findFreePosition(wanted, size, placementObstacles(nodes, subgraphId), placementMinimum(subgraphId));
  };
}

/**
 * Returns a function that adds a module of a type, at a graph position (relative to the subgraph if given) or near the center of the
 * visible graph, moved to where it doesn't overlap other nodes, and resolves to the new module (undefined if adding failed). The module
 * also appears through the engine's change notification, and becomes the only selected element on the graph.
 */
export function useAddModule() {
  const { getNode, setEdges, setNodes } = useReactFlow();
  const visibleCenter = useVisibleCenter();
  const freePosition = useFreeModulePosition();
  const notifyError = useNotifyError();
  const { t } = useTranslation();
  const create = useMutation({
    ...getCreateModuleMutationOptions(),
    onError: (error) => notifyError(t("module.addFailed"), error),
  });

  return async (type: string, position?: XYPosition, subgraphId?: string): Promise<ModuleDto | undefined> => {
    try {
      const wanted = position ?? visibleCenter();
      const module = await create.mutateAsync({
        data: { type, position: freePosition(type, wanted, subgraphId), subgraphId },
      });
      pendingPlacement.set(module.id, wanted);
      if (getNode(module.id)) {
        setNodes((nodes) => nodes.map((node) => ({ ...node, selected: node.id === module.id })));
        setEdges((edges) => edges.map((edge) => (edge.selected ? { ...edge, selected: false } : edge)));
      } else {
        pendingSelection.add(module.id);
      }

      return module;
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
  const { t } = useTranslation();
  const { mutate } = useMutation({
    ...getInstantiateSubgraphTemplateMutationOptions(),
    onError: (error) => notifyError(t("subgraph.addFailed"), error),
  });

  return (templateId: string, position?: XYPosition) =>
    mutate({ id: templateId, data: { position: position ?? visibleCenter() } });
}

/**
 * The module types with their widget titles in the current language, sorted by title, for menus.
 */
export function useModuleTypeChoices(): (ModuleTypeDto & { title: string })[] {
  const { data: moduleTypes = [] } = useGetModuleTypes();
  const { widgets } = usePluginWidgets();
  const language = useLanguage();
  return moduleTypes
    .map((type) => {
      const widget = widgets.get(type.type);
      return { ...type, title: widget ? localize(widget.title) : type.type };
    })
    .sort((a, b) => a.title.localeCompare(b.title, language));
}
