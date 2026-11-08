import type { VoiceChangerState } from "@micser/web-sdk";
import { createTestQueryClient, testModule, TestProviders } from "@micser/web-sdk/testing";
import { expect, test, vi } from "vitest";
import { userEvent } from "vitest/browser";
import { render } from "vitest-browser-react";
import { VoiceChangerWidget } from "../VoiceChangerWidget";

const state: VoiceChangerState = {
  preset: "MaleToFemale",
  pitch: 6,
  formant: 3,
  mode: "Shift",
  targetPitch: 200,
  breathiness: 0.2,
  wobble: 0.1,
  effect: "None",
  mix: 0.8,
  quality: 5,
};

async function renderVoiceChanger(overrides?: Partial<VoiceChangerState>) {
  const setState = vi.fn();
  const screen = await render(
    <TestProviders queryClient={createTestQueryClient()}>
      <VoiceChangerWidget module={testModule("VoiceChanger", { ...state, ...overrides })} setState={setState} />
    </TestProviders>
  );

  return { screen, setState };
}

test("shows the parameters", async () => {
  const { screen } = await renderVoiceChanger();

  await expect.element(screen.getByRole("combobox", { name: "Preset" })).toHaveTextContent("Male to female");
  await expect.element(screen.getByText("+6 st", { exact: true })).toBeVisible();
  await expect.element(screen.getByText("+3 st", { exact: true })).toBeVisible();
  await expect.element(screen.getByText("20%", { exact: true })).toBeVisible();
  await expect.element(screen.getByText("10%", { exact: true })).toBeVisible();
  await expect.element(screen.getByText("80%", { exact: true })).toBeVisible();
  await expect.element(screen.getByRole("combobox", { name: "Effect" })).toHaveTextContent("No effect");
});

test("a preset sets its parameters and keeps mix and quality", async () => {
  const { screen, setState } = await renderVoiceChanger();

  await screen.getByRole("combobox", { name: "Preset" }).click();
  await screen.getByRole("option", { name: "Monster" }).click();

  expect(setState).toHaveBeenCalledWith({
    ...state,
    preset: "Monster",
    pitch: -12,
    formant: -6,
    breathiness: 0,
    wobble: 0,
    effect: "None",
  });
});

test("the robot preset sets the robot effect", async () => {
  const { screen, setState } = await renderVoiceChanger();

  await screen.getByRole("combobox", { name: "Preset" }).click();
  await screen.getByRole("option", { name: "Robot" }).click();

  expect(setState).toHaveBeenCalledWith({
    ...state,
    preset: "Robot",
    pitch: 2,
    formant: -2,
    breathiness: 0,
    wobble: 0,
    effect: "Robot",
  });
});

test("changing the effect makes it custom", async () => {
  const { screen, setState } = await renderVoiceChanger();

  await screen.getByRole("combobox", { name: "Effect" }).click();
  await screen.getByRole("option", { name: "Radio" }).click();

  expect(setState).toHaveBeenCalledWith({ ...state, preset: "Custom", effect: "Radio" });
});

test("custom keeps the parameters", async () => {
  const { screen, setState } = await renderVoiceChanger();

  await screen.getByRole("combobox", { name: "Preset" }).click();
  await screen.getByRole("option", { name: "Custom" }).click();

  expect(setState).toHaveBeenCalledWith({ ...state, preset: "Custom" });
});

test("changing the formant makes it custom, changing the mix doesn't", async () => {
  const { screen, setState } = await renderVoiceChanger();

  screen.getByRole("slider", { name: "Formant" }).element().focus();
  await userEvent.keyboard("{ArrowRight}");
  expect(setState).toHaveBeenLastCalledWith({ ...state, preset: "Custom", formant: 3.5 });

  screen.getByRole("slider", { name: "Mix" }).element().focus();
  await userEvent.keyboard("{ArrowLeft}");
  expect(setState).toHaveBeenLastCalledWith({ ...state, mix: 0.79 });
});

test("target mode shows the target pitch instead of the shift", async () => {
  const { screen } = await renderVoiceChanger({ mode: "Target", targetPitch: 210 });

  await expect.element(screen.getByRole("combobox", { name: "Pitch mode" })).toHaveTextContent("Target pitch");
  await expect.element(screen.getByText("210 Hz", { exact: true })).toBeVisible();
  await expect.element(screen.getByRole("slider", { name: "Pitch" })).not.toBeInTheDocument();
});

test("changing the pitch mode makes it custom", async () => {
  const { screen, setState } = await renderVoiceChanger();

  await screen.getByRole("combobox", { name: "Pitch mode" }).click();
  await screen.getByRole("option", { name: "Target pitch" }).click();

  expect(setState).toHaveBeenCalledWith({ ...state, preset: "Custom", mode: "Target" });
});

test("a preset switches back to shifting by semitones", async () => {
  const { screen, setState } = await renderVoiceChanger({ mode: "Target", targetPitch: 210 });

  await screen.getByRole("combobox", { name: "Preset" }).click();
  await screen.getByRole("option", { name: "Chipmunk" }).click();

  expect(setState).toHaveBeenCalledWith({
    ...state,
    preset: "Chipmunk",
    mode: "Shift",
    targetPitch: 210,
    pitch: 12,
    formant: 12,
    breathiness: 0,
    wobble: 0,
    effect: "None",
  });
});
