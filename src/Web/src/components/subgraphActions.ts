import type { SubgraphDto } from "@micser/web-sdk";
import { createContext, useContext } from "react";

/**
 * Template actions that open dialogs. The dialogs live outside the graph, where React Flow's key and click handling on nodes doesn't
 * reach them.
 */
export interface SubgraphActions {
  manageTemplates: () => void;
  saveAsTemplate: (subgraph: SubgraphDto) => void;
  /** Asks for confirmation first. */
  updateFromTemplate: (subgraph: SubgraphDto) => void;
}

export const SubgraphActionsContext = createContext<SubgraphActions | undefined>(undefined);

export function useSubgraphActions() {
  const actions = useContext(SubgraphActionsContext);
  if (!actions) {
    throw new Error("useSubgraphActions must be used inside a SubgraphActionsProvider.");
  }

  return actions;
}
