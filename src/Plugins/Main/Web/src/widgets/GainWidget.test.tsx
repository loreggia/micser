import { createTestQueryClient, testModule, TestProviders } from "@micser/web-sdk/testing";
import { expect, test, vi } from "vitest";
import { userEvent } from "vitest/browser";
import { render } from "vitest-browser-react";
import { GainWidget } from "./GainWidget";

test("a step changes the gain by half a decibel", async () => {
  const setState = vi.fn();
  const screen = await render(
    <TestProviders queryClient={createTestQueryClient()}>
      <GainWidget module={testModule("Gain", { gain: -6 })} setState={setState} />
    </TestProviders>
  );

  await expect.element(screen.getByText("-6.0 dB")).toBeVisible();

  screen.getByRole("slider", { name: "Gain" }).element().focus();
  await userEvent.keyboard("{ArrowRight}");

  expect(setState).toHaveBeenLastCalledWith({ gain: -5.5 });
});

test("shows German labels and numbers", async () => {
  const screen = await render(
    <TestProviders queryClient={createTestQueryClient()} language="de">
      <GainWidget module={testModule("Gain", { gain: -6 })} setState={vi.fn()} />
    </TestProviders>
  );

  await expect.element(screen.getByText("-6,0 dB")).toBeVisible();
  await expect.element(screen.getByRole("slider", { name: "Verstärkung" })).toBeVisible();
});
