import { afterEach, expect, test, vi } from "vitest";
import { defineTranslations, i18n, localize, resolveLanguage } from "../i18n";

afterEach(async () => {
  vi.unstubAllGlobals();
  await i18n.changeLanguage("en");
});

test("resolveLanguage prefers a supported preference", () => {
  vi.stubGlobal("navigator", { languages: ["en-US"], language: "en-US" });

  expect(resolveLanguage("de")).toBe("de");
});

test("resolveLanguage takes the first supported browser language otherwise", () => {
  vi.stubGlobal("navigator", { languages: ["fr-FR", "de-AT", "en-US"], language: "fr-FR" });

  expect(resolveLanguage(null)).toBe("de");
  expect(resolveLanguage("fr")).toBe("de");
});

test("resolveLanguage falls back to English", () => {
  vi.stubGlobal("navigator", { languages: ["fr-FR"], language: "fr-FR" });

  expect(resolveLanguage(undefined)).toBe("en");
});

test("defineTranslations translates into the current language", async () => {
  const { t } = defineTranslations("test-current", {
    en: { greeting: "Hello {{name}}", group: { items_one: "{{count}} item", items_other: "{{count}} items" } },
    de: { greeting: "Hallo {{name}}", group: { items_one: "{{count}} Element", items_other: "{{count}} Elemente" } },
  });

  expect(t("greeting", { name: "Micser" })).toBe("Hello Micser");
  expect(t("group.items", { count: 2 })).toBe("2 items");

  await i18n.changeLanguage("de");

  expect(t("greeting", { name: "Micser" })).toBe("Hallo Micser");
  expect(t("group.items", { count: 1 })).toBe("1 Element");
});

test("defineTranslations falls back to English for missing translations", async () => {
  const { t } = defineTranslations("test-fallback", {
    en: { only: "English only" },
    de: {} as { only: string },
  });

  await i18n.changeLanguage("de");

  expect(t("only")).toBe("English only");
});

test("localize translates functions and keeps strings", () => {
  expect(localize("Gain")).toBe("Gain");
  expect(localize(() => "Verstärkung")).toBe("Verstärkung");
});
