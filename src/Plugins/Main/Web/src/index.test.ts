import { i18n, isPlugin, localize } from "@micser/web-sdk";
import { readFileSync } from "node:fs";
import { expect, test } from "vitest";
import plugin from "./index";

interface OpenApiDocument {
  components: { schemas: { ModuleDto: { discriminator: { mapping: Record<string, string> } } } };
}

const engineDocument = JSON.parse(
  readFileSync(new URL("../../../../WebSdk/openapi/engine.json", import.meta.url), "utf8")
) as OpenApiDocument;

test("the default export is a plugin", () => {
  expect(isPlugin(plugin)).toBe(true);
});

test("each module type has one widget", () => {
  const moduleTypes = plugin.widgets.map((widget) => widget.moduleType);

  expect(new Set(moduleTypes).size).toBe(moduleTypes.length);
});

test("the widgets cover the engine's module types", () => {
  const engineModuleTypes = Object.keys(engineDocument.components.schemas.ModuleDto.discriminator.mapping);

  expect(plugin.widgets.map((widget) => widget.moduleType).toSorted()).toEqual(engineModuleTypes.toSorted());
});

test("the widget titles are translated", async () => {
  const gain = plugin.widgets.find((widget) => widget.moduleType === "Gain")!;

  expect(localize(gain.title)).toBe("Gain");
  await i18n.changeLanguage("de");
  try {
    expect(localize(gain.title)).toBe("Verstärkung");
  } finally {
    await i18n.changeLanguage("en");
  }
});
