import type { Node, Rect, XYPosition } from "@xyflow/react";
import { gridSize, subgraphPadding } from "./subgraphs";

/** Space kept between a placed node and the others. */
const spacing = gridSize;

/** How far from the wanted position the search goes, in grid steps. */
const maxRadius = 50;

function overlaps(a: Rect, b: Rect) {
  return (
    a.x < b.x + b.width + spacing &&
    a.x + a.width + spacing > b.x &&
    a.y < b.y + b.height + spacing &&
    a.y + a.height + spacing > b.y
  );
}

/**
 * The grid position nearest to the wanted one where a node of the size overlaps none of the obstacles (keeping some space), and isn't
 * left of or above the minimum if given. It searches rings of grid positions around the wanted one; if all of them are taken, the wanted
 * position is used.
 */
export function findFreePosition(
  wanted: XYPosition,
  size: { width: number; height: number },
  obstacles: Rect[],
  minimum?: XYPosition
): XYPosition {
  const start = { x: Math.round(wanted.x / gridSize) * gridSize, y: Math.round(wanted.y / gridSize) * gridSize };
  if (minimum) {
    start.x = Math.max(start.x, minimum.x);
    start.y = Math.max(start.y, minimum.y);
  }

  const isFree = (position: XYPosition) =>
    (!minimum || (position.x >= minimum.x && position.y >= minimum.y)) &&
    !obstacles.some((obstacle) => overlaps({ ...position, ...size }, obstacle));

  for (let radius = 0; radius <= maxRadius; radius++) {
    const ring: XYPosition[] = [];
    for (let dx = -radius; dx <= radius; dx++) {
      for (let dy = -radius; dy <= radius; dy++) {
        if (Math.max(Math.abs(dx), Math.abs(dy)) === radius) {
          ring.push({ x: start.x + dx * gridSize, y: start.y + dy * gridSize });
        }
      }
    }

    // within a ring, the nearest positions first
    ring.sort((a, b) => Math.hypot(a.x - start.x, a.y - start.y) - Math.hypot(b.x - start.x, b.y - start.y));
    const free = ring.find(isFree);
    if (free) {
      return free;
    }
  }

  return start;
}

/**
 * The nodes a new module avoids: in a subgraph only the subgraph's modules (positions relative to it), otherwise the modules and subgraphs
 * outside subgraphs. Hidden nodes and the module itself are left out.
 */
export function placementObstacles(nodes: Node[], subgraphId: string | undefined, excludeId?: string): Rect[] {
  return nodes
    .filter(
      (node) => !node.hidden && node.id !== excludeId && (subgraphId ? node.parentId === subgraphId : !node.parentId)
    )
    .map((node) => ({
      ...node.position,
      width: node.measured?.width ?? node.width ?? 0,
      height: node.measured?.height ?? node.height ?? 0,
    }));
}

/** In a subgraph, the top left position a module may have: inside the frame's padding and below its header (as when grouping). */
export function placementMinimum(subgraphId: string | undefined): XYPosition | undefined {
  return subgraphId ? { x: subgraphPadding, y: subgraphPadding + gridSize } : undefined;
}
