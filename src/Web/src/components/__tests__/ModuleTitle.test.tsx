import { FluentProvider, webLightTheme } from "@fluentui/react-components";
import { expect, test, vi } from "vitest";
import { userEvent } from "vitest/browser";
import { render } from "vitest-browser-react";
import { ModuleTitle } from "../ModuleTitle";

async function renderTitle(name: string | null) {
  const onRename = vi.fn<(name: string | null) => void>();
  const screen = await render(
    <FluentProvider theme={webLightTheme}>
      <ModuleTitle name={name} fallback="Gain" label="Module name" onRename={onRename} />
    </FluentProvider>
  );

  const field = screen.getByRole("textbox", { name: "Module name" });
  const startRenaming = () => screen.getByText(name || "Gain").dblClick();
  return { screen, field, onRename, startRenaming };
}

test("shows the name, or the fallback without one", async () => {
  await expect.element((await renderTitle("Mic")).screen.getByText("Mic")).toBeVisible();
  await expect.element((await renderTitle(null)).screen.getByText("Gain")).toBeVisible();
});

test("double-click edits the name and Enter saves it once", async () => {
  const { screen, field, onRename, startRenaming } = await renderTitle("Mic");

  await startRenaming();
  await expect.element(field).toHaveValue("Mic");
  await expect.element(field).toHaveFocus();
  await field.fill("Voice");
  await userEvent.keyboard("{Enter}");

  await expect.element(field).not.toBeInTheDocument();
  expect(onRename).toHaveBeenCalledExactlyOnceWith("Voice");
  // the name comes back through the module, which the test doesn't change
  await expect.element(screen.getByText("Mic")).toBeVisible();
});

test("leaving the field saves", async () => {
  const { field, onRename, startRenaming } = await renderTitle(null);

  await startRenaming();
  await field.fill("  Voice  ");
  field.element().blur();

  await expect.element(field).not.toBeInTheDocument();
  expect(onRename).toHaveBeenCalledExactlyOnceWith("Voice");
});

test("Escape cancels", async () => {
  const { field, onRename, startRenaming } = await renderTitle("Mic");

  await startRenaming();
  await field.fill("Voice");
  await userEvent.keyboard("{Escape}");

  await expect.element(field).not.toBeInTheDocument();
  expect(onRename).not.toHaveBeenCalled();
});

test("an empty name goes back to the fallback", async () => {
  const { field, onRename, startRenaming } = await renderTitle("Mic");

  await startRenaming();
  await field.fill(" ");
  await userEvent.keyboard("{Enter}");

  expect(onRename).toHaveBeenCalledExactlyOnceWith(null);
});

test("an unchanged name isn't saved", async () => {
  const { onRename, startRenaming } = await renderTitle("Mic");

  await startRenaming();
  await userEvent.keyboard("{Enter}");

  expect(onRename).not.toHaveBeenCalled();
});
