import { createTestQueryClient, testModule, TestProviders } from "@micser/web-sdk/testing";
import { expect, test, vi } from "vitest";
import { userEvent } from "vitest/browser";
import { render } from "vitest-browser-react";
import { PitchWidget } from "./PitchWidget";

async function renderPitch(pitch: number) {
  const setState = vi.fn();
  const screen = await render(
    <TestProviders queryClient={createTestQueryClient()}>
      <PitchWidget module={testModule("Pitch", { pitch, quality: 5 })} setState={setState} />
    </TestProviders>
  );

  return { screen, setState };
}

test("shows the pitch in semitones", async () => {
  await expect.element((await renderPitch(2 / 12)).screen.getByText("+2 st")).toBeVisible();
  await expect.element((await renderPitch(-1)).screen.getByText("-12 st")).toBeVisible();
});

test("a step is one semitone", async () => {
  const { screen, setState } = await renderPitch(0);

  screen.getByRole("slider", { name: "Pitch" }).element().focus();
  await userEvent.keyboard("{ArrowLeft}");

  const [{ pitch, quality }] = setState.mock.lastCall as [{ pitch: number; quality: number }];
  expect(pitch * 12).toBeCloseTo(-1);
  expect(quality).toBe(5);
});
