import type { Node } from "@xyflow/react";
import { describe, expect, test } from "vitest";
import { findFreePosition, placementMinimum, placementObstacles } from "./placement";
import { gridSize, subgraphPadding } from "./subgraphs";

const size = { width: 100, height: 50 };

describe("findFreePosition", () => {
  test("snaps a free position to the grid", () => {
    expect(findFreePosition({ x: 13, y: 27 }, size, [])).toEqual({ x: 20, y: 20 });
  });

  test("moves to the nearest position that keeps a grid step from the obstacle", () => {
    const obstacle = { x: 0, y: 0, ...size };

    // above the obstacle: its height plus a grid step of spacing, on the grid
    expect(findFreePosition({ x: 0, y: 0 }, size, [obstacle])).toEqual({ x: 0, y: -80 });
  });

  test("stays right of and below the minimum", () => {
    const obstacle = { x: 0, y: 0, ...size };

    expect(findFreePosition({ x: 0, y: 0 }, size, [obstacle], { x: 0, y: 0 })).toEqual({ x: 0, y: 80 });
    expect(findFreePosition({ x: -50, y: -50 }, size, [], { x: 40, y: 60 })).toEqual({ x: 40, y: 60 });
  });

  test("falls back to the wanted position when nothing is free", () => {
    const everything = { x: -100_000, y: -100_000, width: 200_000, height: 200_000 };

    expect(findFreePosition({ x: 31, y: 9 }, size, [everything])).toEqual({ x: 40, y: 0 });
  });
});

function node(id: string, properties: Partial<Node> = {}): Node {
  return { id, position: { x: 0, y: 0 }, data: {}, ...properties };
}

describe("placementObstacles", () => {
  const nodes = [
    node("top", { position: { x: 10, y: 20 }, measured: { width: 100, height: 60 }, width: 1, height: 1 }),
    node("subgraph", { width: 300, height: 200 }),
    node("hidden", { hidden: true }),
    node("inside", { parentId: "subgraph", position: { x: 40, y: 60 } }),
  ];

  test("outside subgraphs, takes the visible top-level nodes with their measured size", () => {
    expect(placementObstacles(nodes, undefined)).toEqual([
      { x: 10, y: 20, width: 100, height: 60 },
      { x: 0, y: 0, width: 300, height: 200 },
    ]);
  });

  test("in a subgraph, takes only its modules", () => {
    expect(placementObstacles(nodes, "subgraph")).toEqual([{ x: 40, y: 60, width: 0, height: 0 }]);
  });

  test("leaves out the excluded node", () => {
    expect(placementObstacles(nodes, undefined, "top")).toHaveLength(1);
  });
});

describe("placementMinimum", () => {
  test("is inside the padding and below the header in a subgraph", () => {
    expect(placementMinimum("subgraph")).toEqual({ x: subgraphPadding, y: subgraphPadding + gridSize });
    expect(placementMinimum(undefined)).toBeUndefined();
  });
});
