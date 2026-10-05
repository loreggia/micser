import { developmentShell, expect, FakeShell, test } from "../fixtures";

test("the language preference translates the UI, the widgets and numbers", async ({ graph, engine, page }) => {
  await engine.setPreferences({ language: "de" });
  const module = await engine.addModule("Gain", { x: 0, y: 0 });
  await graph.open();

  await expect(page.getByRole("button", { name: "Modul hinzufügen" })).toBeVisible();
  await expect(graph.title(graph.node(module.id))).toHaveText("Verstärkung");
  await expect(graph.node(module.id).getByText("0,0 dB")).toBeVisible();
  await expect(page.locator("html")).toHaveAttribute("lang", "de");
});

test("the language changes in the settings right away", async ({ graph, engine, page }) => {
  await graph.open();
  await page.getByRole("button", { name: "Settings" }).click();
  // its name changes with the language
  const dialog = page.getByRole("dialog");

  await dialog.getByRole("combobox", { name: "Language" }).click();
  await page.getByRole("option", { name: "Deutsch" }).click();

  await expect(page.getByRole("dialog", { name: "Einstellungen" })).toBeVisible();
  await expect.poll(async () => (await engine.preferences()).language).toBe("de");

  await dialog.getByRole("combobox", { name: "Sprache" }).click();
  await page.getByRole("option", { name: /^System/ }).click();

  await expect(page.getByRole("dialog", { name: "Settings" })).toBeVisible();
  await expect.poll(async () => (await engine.preferences()).language).toBeNull();
});

test.describe("with a German browser", () => {
  test.use({ locale: "de-DE" });

  test("follows the browser's language without a preference", async ({ graph, engine, page }) => {
    await graph.open();

    await expect(page.getByRole("button", { name: "Modul hinzufügen" })).toBeVisible();

    await engine.setPreferences({ language: "en" });

    await expect(page.getByRole("button", { name: "Add module" })).toBeVisible();
  });
});

test("passes the language preference to the shell", async ({ graph, engine }) => {
  const shell = await FakeShell.install(graph.page, developmentShell);
  await graph.open();
  await expect.poll(() => shell.languages()).toEqual([null]);

  await engine.setPreferences({ language: "de" });

  await expect.poll(() => shell.languages()).toEqual([null, "de"]);
});
