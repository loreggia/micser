import { useNodeId, useNodesInitialized, useUpdateNodeInternals } from "@xyflow/react";
import { useEffect, useRef } from "react";

/**
 * Has React Flow measure the node's connectors again when they change. It measures them only when the node resizes, so a connector that
 * appears in an existing row (e.g. an output channel next to an input channel) couldn't start a connection. Until all nodes are measured,
 * React Flow measures them itself, and an update would run the initial fitView with only the nodes measured so far.
 */
export function useHandlesChanged(handleIds: readonly string[]) {
  const nodeId = useNodeId();
  const updateNodeInternals = useUpdateNodeInternals();
  const initialized = useNodesInitialized();
  const key = handleIds.join("\n");
  const measuredKey = useRef(key);
  useEffect(() => {
    if (nodeId && initialized && key !== measuredKey.current) {
      measuredKey.current = key;
      updateNodeInternals(nodeId);
    }
  }, [nodeId, initialized, key, updateNodeInternals]);
}
