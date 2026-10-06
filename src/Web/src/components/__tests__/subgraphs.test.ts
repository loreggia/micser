import { describe, expect, test } from "vitest";
import { frameAround, minSubgraphSize, proxyHandleId, resolveHandle } from "../subgraphs";

describe("frameAround", () => {
  test("puts the frame on the grid with padding and room for the header", () => {
    expect(frameAround({ x: 0, y: 0, width: 400, height: 300 })).toEqual({
      position: { x: -40, y: -60 },
      size: { width: 480, height: 400 },
    });
  });

  test("rounds outwards to the grid", () => {
    const { position, size } = frameAround({ x: 105, y: 113, width: 290, height: 190 });

    expect(position).toEqual({ x: 60, y: 40 });
    expect(position.x + size.width).toBe(440);
    expect(position.y + size.height).toBe(360);
  });

  test("is at least the minimum size", () => {
    expect(frameAround({ x: 100, y: 100, width: 10, height: 10 }).size).toEqual(minSubgraphSize);
  });
});

describe("proxy handles", () => {
  test("resolve to the module and port", () => {
    expect(resolveHandle("subgraph", proxyHandleId("out", "module-1", "Output"))).toEqual({
      moduleId: "module-1",
      port: "Output",
    });
  });

  test("keep colons in the port name", () => {
    expect(resolveHandle("subgraph", proxyHandleId("in", "module-1", "a:b"))).toEqual({
      moduleId: "module-1",
      port: "a:b",
    });
  });

  test("other handles belong to the node itself", () => {
    expect(resolveHandle("module-2", "Input")).toEqual({ moduleId: "module-2", port: "Input" });
    expect(resolveHandle("module-2", null)).toEqual({ moduleId: "module-2", port: "" });
  });
});
