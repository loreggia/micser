import type { DuckerState } from "@micser/web-sdk";
import { createTestQueryClient, testModule, TestProviders } from "@micser/web-sdk/testing";
import { expect, test, vi } from "vitest";
import { userEvent } from "vitest/browser";
import { render } from "vitest-browser-react";
import { DuckerWidget } from "../DuckerWidget";

const state: DuckerState = {
  threshold: -40,
  depth: -15,
  attack: 0.01,
  hold: 0.3,
  release: 0.5,
};

async function renderDucker() {
  const setState = vi.fn();
  const screen = await render(
    <TestProviders queryClient={createTestQueryClient()}>
      <DuckerWidget module={testModule("Ducker", state)} setState={setState} />
    </TestProviders>
  );

  return { screen, setState };
}

test("shows the parameters with their units", async () => {
  const { screen } = await renderDucker();

  for (const text of ["-40.0 dB", "-15.0 dB", "10 ms", "300 ms", "500 ms"]) {
    await expect.element(screen.getByText(text, { exact: true })).toBeVisible();
  }
});

test("changes one parameter and keeps the others", async () => {
  const { screen, setState } = await renderDucker();

  screen.getByRole("slider", { name: "Depth" }).element().focus();
  await userEvent.keyboard("{ArrowLeft}");

  expect(setState).toHaveBeenLastCalledWith({ ...state, depth: -15.5 });
});
