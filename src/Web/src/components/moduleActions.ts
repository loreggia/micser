import type { ModuleDto } from "@micser/web-sdk";
import { createContext, useContext } from "react";

/**
 * Module actions that open dialogs. The dialogs live outside the graph, where React Flow's key and click handling on nodes doesn't
 * reach them.
 */
export interface ModuleActions {
  /** Asks for a channel count other than the presets of the "Channels" menu. */
  chooseChannelCount: (module: ModuleDto) => void;
}

export const ModuleActionsContext = createContext<ModuleActions | undefined>(undefined);

export function useModuleActions() {
  const actions = useContext(ModuleActionsContext);
  if (!actions) {
    throw new Error("useModuleActions must be used inside a ModuleActionsProvider.");
  }

  return actions;
}
