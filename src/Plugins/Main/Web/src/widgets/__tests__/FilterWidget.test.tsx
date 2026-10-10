import type { FilterState, Language } from "@micser/web-sdk";
import { createTestQueryClient, testModule, TestProviders } from "@micser/web-sdk/testing";
import { expect, test, vi } from "vitest";
import { userEvent } from "vitest/browser";
import { render } from "vitest-browser-react";
import { FilterWidget } from "../FilterWidget";

const state: FilterState = { type: "HighPass", frequency: 80, slope: 24, q: 0.71 };

async function renderFilter(language?: Language) {
  const setState = vi.fn();
  const screen = await render(
    <TestProviders queryClient={createTestQueryClient()} language={language}>
      <FilterWidget module={testModule("Filter", state)} setState={setState} />
    </TestProviders>
  );

  return { screen, setState };
}

test("shows the parameters with their units", async () => {
  const { screen } = await renderFilter();

  await expect.element(screen.getByRole("combobox")).toHaveTextContent("High-pass");
  await expect.element(screen.getByText("80 Hz", { exact: true })).toBeVisible();
  await expect.element(screen.getByText("24 dB/oct", { exact: true })).toBeVisible();
  await expect.element(screen.getByText("0.71", { exact: true })).toBeVisible();
});

test("shows the frequency response", async () => {
  const { screen } = await renderFilter();

  await expect.element(screen.getByRole("img", { name: "Frequency response" })).toBeVisible();
});

test("shows the frequency response in German", async () => {
  const { screen } = await renderFilter("de");

  await expect.element(screen.getByRole("img", { name: "Frequenzgang" })).toBeVisible();
});

test("changes the type and keeps the other parameters", async () => {
  const { screen, setState } = await renderFilter();

  await screen.getByRole("combobox").click();
  await screen.getByRole("option", { name: "Low-pass" }).click();

  expect(setState).toHaveBeenCalledWith({ ...state, type: "LowPass" });
});

test("a step changes the slope by 12 dB per octave", async () => {
  const { screen, setState } = await renderFilter();

  screen.getByRole("slider", { name: "Slope" }).element().focus();
  await userEvent.keyboard("{ArrowRight}");

  expect(setState).toHaveBeenLastCalledWith({ ...state, slope: 36 });
});

test("changes the Q and keeps the other parameters", async () => {
  const { screen, setState } = await renderFilter();

  screen.getByRole("slider", { name: "Q" }).element().focus();
  await userEvent.keyboard("{End}");

  expect(setState).toHaveBeenLastCalledWith({ ...state, q: 10 });
});

test("shows German labels", async () => {
  const { screen } = await renderFilter("de");

  await expect.element(screen.getByRole("combobox")).toHaveTextContent("Hochpass");
  await expect.element(screen.getByText("24 dB/Okt.", { exact: true })).toBeVisible();
  await expect.element(screen.getByText("0,71", { exact: true })).toBeVisible();
});
