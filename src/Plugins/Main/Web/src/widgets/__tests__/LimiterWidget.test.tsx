import type { LimiterState } from "@micser/web-sdk";
import { createTestQueryClient, testModule, TestProviders } from "@micser/web-sdk/testing";
import { expect, test, vi } from "vitest";
import { userEvent } from "vitest/browser";
import { render } from "vitest-browser-react";
import { LimiterWidget } from "../LimiterWidget";

const state: LimiterState = {
  ceiling: -1,
  gain: 6,
  release: 0.1,
};

async function renderLimiter() {
  const setState = vi.fn();
  const screen = await render(
    <TestProviders queryClient={createTestQueryClient()}>
      <LimiterWidget module={testModule("Limiter", state)} setState={setState} />
    </TestProviders>
  );

  return { screen, setState };
}

test("shows the parameters and the latency", async () => {
  const { screen } = await renderLimiter();

  for (const text of ["-1.0 dB", "+6.0 dB", "100 ms", "Adds 5 ms latency"]) {
    await expect.element(screen.getByText(text, { exact: true })).toBeVisible();
  }
});

test("changes one parameter and keeps the others", async () => {
  const { screen, setState } = await renderLimiter();

  screen.getByRole("slider", { name: "Ceiling" }).element().focus();
  await userEvent.keyboard("{ArrowLeft}");

  expect(setState).toHaveBeenLastCalledWith({ ...state, ceiling: expect.closeTo(-1.1, 6) as number });
});
