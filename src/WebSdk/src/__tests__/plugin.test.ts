import { expect, test } from "vitest";
import { definePlugin, isPlugin } from "../plugin";

test("isPlugin accepts a plugin", () => {
  expect(isPlugin(definePlugin({ name: "Test", widgets: [] }))).toBe(true);
});

test.each([undefined, null, 42, {}, { name: "Test" }, { widgets: [] }, { name: 1, widgets: [] }])(
  "isPlugin rejects %o",
  (value) => {
    expect(isPlugin(value)).toBe(false);
  }
);
