import type { XYPosition } from "@xyflow/react";

/**
 * Modules to select once they appear on the graph: a new module reaches the graph through the engine's notification, which can come
 * before or after the request that created it returns.
 */
export const pendingSelection = new Set<string>();

/**
 * New modules by id, with the position they were wanted at, to place again once their size is measured: before that, placement assumes
 * the size of a module of the same type on the graph, or a default.
 */
export const pendingPlacement = new Map<string, XYPosition>();
