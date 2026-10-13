import type { StereoToolsState } from "@micser/web-sdk";
import { createTestQueryClient, testModule, TestProviders } from "@micser/web-sdk/testing";
import { expect, test, vi } from "vitest";
import { userEvent } from "vitest/browser";
import { render } from "vitest-browser-react";
import { StereoToolsWidget } from "../StereoToolsWidget";

const state: StereoToolsState = {
  mode: "Stereo",
  width: 1,
  balance: -0.25,
  invertLeft: false,
  invertRight: true,
};

async function renderStereoTools(overrides?: Partial<StereoToolsState>) {
  const setState = vi.fn();
  const screen = await render(
    <TestProviders queryClient={createTestQueryClient()}>
      <StereoToolsWidget module={testModule("StereoTools", { ...state, ...overrides })} setState={setState} />
    </TestProviders>
  );

  return { screen, setState };
}

test("shows the parameters", async () => {
  const { screen } = await renderStereoTools();

  await expect.element(screen.getByRole("combobox", { name: "Mode" })).toHaveTextContent("Stereo");
  await expect.element(screen.getByText("100%", { exact: true })).toBeVisible();
  await expect.element(screen.getByText("Left 25%", { exact: true })).toBeVisible();
  await expect.element(screen.getByRole("switch", { name: "Invert left polarity" })).not.toBeChecked();
  await expect.element(screen.getByRole("switch", { name: "Invert right polarity" })).toBeChecked();
});

test("shows a centered balance", async () => {
  const { screen } = await renderStereoTools({ balance: 0 });

  await expect.element(screen.getByText("Center", { exact: true })).toBeVisible();
});

test("changes the mode and keeps the other parameters", async () => {
  const { screen, setState } = await renderStereoTools();

  await screen.getByRole("combobox", { name: "Mode" }).click();
  await screen.getByRole("option", { name: "Left on both sides" }).click();

  expect(setState).toHaveBeenCalledWith({ ...state, mode: "LeftToBoth" });
});

test("changes the balance and the polarity", async () => {
  const { screen, setState } = await renderStereoTools();

  screen.getByRole("slider", { name: "Balance" }).element().focus();
  await userEvent.keyboard("{ArrowRight}");
  expect(setState).toHaveBeenLastCalledWith({ ...state, balance: -0.24 });

  await screen.getByRole("switch", { name: "Invert left polarity" }).click();
  expect(setState).toHaveBeenLastCalledWith({ ...state, invertLeft: true });
});
