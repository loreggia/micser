import type { CompressorState } from "@micser/web-sdk";
import { createTestQueryClient, testModule, TestProviders } from "@micser/web-sdk/testing";
import { expect, test, vi } from "vitest";
import { userEvent } from "vitest/browser";
import { render } from "vitest-browser-react";
import { CompressorWidget } from "../CompressorWidget";

const state: CompressorState = {
  type: "Downward",
  amount: 1,
  attack: 0.01,
  release: 0.1,
  ratio: 4,
  threshold: -20,
  knee: 6,
  makeUpGain: 0,
};

async function renderCompressor() {
  const setState = vi.fn();
  const screen = await render(
    <TestProviders queryClient={createTestQueryClient()}>
      <CompressorWidget
        module={testModule("Compressor", state)}
        setState={setState}
        defaultState={{ ...state, threshold: -10, ratio: 2 }}
      />
    </TestProviders>
  );

  return { screen, setState };
}

test("shows the parameters with their units", async () => {
  const { screen } = await renderCompressor();

  for (const text of ["-20.0 dB", "4.0:1", "10 ms", "100 ms", "6 dB", "100%"]) {
    await expect.element(screen.getByText(text, { exact: true })).toBeVisible();
  }
});

test("changes the type and keeps the other parameters", async () => {
  const { screen, setState } = await renderCompressor();

  await screen.getByRole("combobox").click();
  await screen.getByRole("option", { name: "Upward" }).click();

  expect(setState).toHaveBeenCalledWith({ ...state, type: "Upward" });
});

test("changes one parameter and keeps the others", async () => {
  const { screen, setState } = await renderCompressor();

  screen.getByRole("slider", { name: "Threshold" }).element().focus();
  await userEvent.keyboard("{ArrowLeft}");

  expect(setState).toHaveBeenLastCalledWith({ ...state, threshold: -20.5 });
});

test("Delete resets a parameter to the module type's default", async () => {
  const { screen, setState } = await renderCompressor();

  screen.getByRole("slider", { name: "Ratio" }).element().focus();
  await userEvent.keyboard("{Delete}");

  expect(setState).toHaveBeenLastCalledWith({ ...state, ratio: 2 });
});

test("explains the type and the parameters behind info buttons", async () => {
  const { screen } = await renderCompressor();

  await screen.getByRole("button", { name: "About Type" }).click();
  await expect.element(screen.getByText(/^Downward makes the parts above the threshold quieter/)).toBeVisible();
  // the popover covers the knobs below the dropdown
  await userEvent.keyboard("{Escape}");

  await screen.getByRole("button", { name: "Ratio information" }).click();
  await expect.element(screen.getByText(/^How strongly levels beyond the threshold are changed/)).toBeVisible();
});
