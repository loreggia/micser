import { getCreateModuleMutationOptions } from "@micser/web-sdk";
import { useMutation } from "@tanstack/react-query";
import { useReactFlow } from "@xyflow/react";
import { useNotifyError } from "../notifications";

/**
 * Returns a function that adds a module of a type near the center of the visible graph.
 * The new module appears through the engine's change notification.
 */
export function useAddModule() {
  const { screenToFlowPosition } = useReactFlow();
  const notifyError = useNotifyError();
  const create = useMutation({
    ...getCreateModuleMutationOptions(),
    onError: (error) => notifyError("Adding the module failed", error),
  });

  return (type: string) => {
    const pane = document.querySelector(".react-flow")?.getBoundingClientRect();
    const position = pane
      ? screenToFlowPosition({ x: pane.left + pane.width / 2 - 120, y: pane.top + pane.height / 3 })
      : { x: 0, y: 0 };

    create.mutate({ data: { type, position } });
  };
}
