import type { EqualizerBand } from "@micser/web-sdk";
import { createTestQueryClient, testModule, TestProviders } from "@micser/web-sdk/testing";
import { expect, test, vi } from "vitest";
import { userEvent } from "vitest/browser";
import { render } from "vitest-browser-react";
import { EqualizerWidget } from "./EqualizerWidget";

const low: EqualizerBand = { frequency: 100, gain: -3, q: 0.7 };
const high: EqualizerBand = { frequency: 8000, gain: 2, q: 1 };

async function renderEqualizer(bands: EqualizerBand[]) {
  const setState = vi.fn();
  const screen = await render(
    <TestProviders queryClient={createTestQueryClient()}>
      <EqualizerWidget module={testModule("Equalizer", { bands })} setState={setState} />
    </TestProviders>
  );

  return { screen, setState };
}

test("shows each band's values", async () => {
  const { screen } = await renderEqualizer([low, high]);

  await expect.element(screen.getByText("Band 2")).toBeVisible();
  await expect.element(screen.getByText("8 kHz")).toBeVisible();
  await expect.element(screen.getByText("-3.0 dB")).toBeVisible();
});

test("adds a band at 1 kHz", async () => {
  const { screen, setState } = await renderEqualizer([low]);

  await screen.getByRole("button", { name: "Add band" }).click();

  expect(setState).toHaveBeenCalledWith({ bands: [low, { frequency: 1000, gain: 0, q: 1.41 }] });
});

test("removes a band", async () => {
  const { screen, setState } = await renderEqualizer([low, high]);

  await screen.getByRole("button", { name: "Remove band 1" }).click();

  expect(setState).toHaveBeenCalledWith({ bands: [high] });
});

test("changes one band and keeps the others", async () => {
  const { screen, setState } = await renderEqualizer([low, high]);

  screen.getByRole("slider", { name: "Gain" }).nth(1).element().focus();
  await userEvent.keyboard("{ArrowRight}");

  expect(setState).toHaveBeenLastCalledWith({ bands: [low, { ...high, gain: 2.5 }] });
});

test("has at most 32 bands", async () => {
  const { screen } = await renderEqualizer(Array.from({ length: 32 }, () => low));

  await expect.element(screen.getByRole("button", { name: "Add band" })).toBeDisabled();
});
