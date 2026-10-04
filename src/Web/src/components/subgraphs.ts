/** A connector of a collapsed subgraph, standing in for a port of one of its modules. */
export interface ProxyPort {
  /** The handle id, see {@link proxyHandleId}. */
  id: string;
  label: string;
}

import type { Rect, XYPosition } from "@xyflow/react";

/** The grid modules snap to; the background dots use the same spacing. */
export const gridSize = 20;

/** The smallest size of an expanded subgraph. */
export const minSubgraphSize = { width: 240, height: 120 };

/** Space between a subgraph's frame and its modules; above them, the frame's header takes another grid row. */
export const subgraphPadding = 2 * gridSize;

/** A subgraph's frame around a rectangle of modules (in graph coordinates), on the grid with room for the header. */
export function frameAround(bounds: Rect): { position: XYPosition; size: { width: number; height: number } } {
  const x = Math.floor((bounds.x - subgraphPadding) / gridSize) * gridSize;
  const y = Math.floor((bounds.y - subgraphPadding - gridSize) / gridSize) * gridSize;
  const right = Math.ceil((bounds.x + bounds.width + subgraphPadding) / gridSize) * gridSize;
  const bottom = Math.ceil((bounds.y + bounds.height + subgraphPadding) / gridSize) * gridSize;
  return {
    position: { x, y },
    size: { width: Math.max(right - x, minSubgraphSize.width), height: Math.max(bottom - y, minSubgraphSize.height) },
  };
}

/** The handle id of a proxy port: the direction, the module and its port. */
export function proxyHandleId(direction: "in" | "out", moduleId: string, port: string) {
  return `${direction}:${moduleId}:${port}`;
}

/** The module and port of a handle, which is a proxy port if the node is a collapsed subgraph. */
export function resolveHandle(nodeId: string, handleId: string | null | undefined): { moduleId: string; port: string } {
  const match = handleId?.match(/^(?:in|out):([^:]+):(.*)$/);
  return match ? { moduleId: match[1], port: match[2] } : { moduleId: nodeId, port: handleId ?? "" };
}
