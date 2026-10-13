import type { NoiseGateState } from "@micser/web-sdk";
import { createTestQueryClient, testModule, TestProviders } from "@micser/web-sdk/testing";
import { expect, test, vi } from "vitest";
import { userEvent } from "vitest/browser";
import { render } from "vitest-browser-react";
import { NoiseGateWidget } from "../NoiseGateWidget";

const state: NoiseGateState = {
  threshold: -50,
  hysteresis: 4,
  range: -40,
  attack: 0.002,
  hold: 0.15,
  release: 0.2,
};

async function renderNoiseGate() {
  const setState = vi.fn();
  const screen = await render(
    <TestProviders queryClient={createTestQueryClient()}>
      <NoiseGateWidget module={testModule("NoiseGate", state)} setState={setState} />
    </TestProviders>
  );

  return { screen, setState };
}

test("shows the parameters with their units", async () => {
  const { screen } = await renderNoiseGate();

  for (const text of ["-50.0 dB", "-40.0 dB", "2 ms", "150 ms", "200 ms", "4 dB"]) {
    await expect.element(screen.getByText(text, { exact: true })).toBeVisible();
  }
});

test("changes one parameter and keeps the others", async () => {
  const { screen, setState } = await renderNoiseGate();

  screen.getByRole("slider", { name: "Threshold" }).element().focus();
  await userEvent.keyboard("{ArrowRight}");

  expect(setState).toHaveBeenLastCalledWith({ ...state, threshold: -49.5 });
});
