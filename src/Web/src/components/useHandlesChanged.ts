import { useNodeId, useUpdateNodeInternals } from "@xyflow/react";
import { useEffect } from "react";

/**
 * Has React Flow measure the node's connectors again when they change. It measures them only when the node resizes, so a connector that
 * appears in an existing row (e.g. an output channel next to an input channel) couldn't start a connection.
 */
export function useHandlesChanged(handleIds: readonly string[]) {
  const nodeId = useNodeId();
  const updateNodeInternals = useUpdateNodeInternals();
  const key = handleIds.join("\n");
  useEffect(() => {
    if (nodeId) {
      updateNodeInternals(nodeId);
    }
  }, [nodeId, key, updateNodeInternals]);
}
