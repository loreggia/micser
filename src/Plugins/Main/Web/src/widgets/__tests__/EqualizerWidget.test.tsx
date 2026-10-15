import type { EqualizerBand } from "@micser/web-sdk";
import { createTestQueryClient, testModule, TestProviders } from "@micser/web-sdk/testing";
import { expect, test, vi } from "vitest";
import { userEvent } from "vitest/browser";
import { render } from "vitest-browser-react";
import { EqualizerWidget } from "../EqualizerWidget";

const low: EqualizerBand = { type: "Peaking", frequency: 100, gain: -3, q: 0.7 };
const high: EqualizerBand = { type: "Peaking", frequency: 8000, gain: 2, q: 1 };

async function renderEqualizer(bands: EqualizerBand[], preamp = 0) {
  const setState = vi.fn();
  const screen = await render(
    <TestProviders queryClient={createTestQueryClient()}>
      <EqualizerWidget module={testModule("Equalizer", { bands, preamp })} setState={setState} />
    </TestProviders>
  );

  return { screen, setState };
}

test("shows each band's values and the preamp", async () => {
  const { screen } = await renderEqualizer([low, high], -4);

  await expect.element(screen.getByText("Band 2")).toBeVisible();
  await expect.element(screen.getByText("8 kHz")).toBeVisible();
  await expect.element(screen.getByText("-3.0 dB")).toBeVisible();
  await expect.element(screen.getByText("-4.0 dB")).toBeVisible();
  await expect.element(screen.getByRole("combobox", { name: "Type of band 1" })).toHaveTextContent("Peaking");
});

test("shows the frequency response with a marker per band", async () => {
  const { screen } = await renderEqualizer([low, high]);

  const graph = screen.getByRole("img", { name: "Frequency response" });
  await expect.element(graph).toBeVisible();
  expect(graph.element().querySelectorAll("circle")).toHaveLength(2);
});

test("adds a peaking band at 1 kHz", async () => {
  const { screen, setState } = await renderEqualizer([low]);

  await screen.getByRole("button", { name: "Add band" }).click();

  expect(setState).toHaveBeenCalledWith({
    bands: [low, { type: "Peaking", frequency: 1000, gain: 0, q: 1.41 }],
    preamp: 0,
  });
});

test("removes a band", async () => {
  const { screen, setState } = await renderEqualizer([low, high]);

  await screen.getByRole("button", { name: "Remove band 1" }).click();

  expect(setState).toHaveBeenCalledWith({ bands: [high], preamp: 0 });
});

test("changes one band and keeps the others", async () => {
  const { screen, setState } = await renderEqualizer([low, high]);

  screen.getByRole("slider", { name: "Gain" }).nth(1).element().focus();
  await userEvent.keyboard("{ArrowRight}");

  expect(setState).toHaveBeenLastCalledWith({ bands: [low, { ...high, gain: 2.5 }], preamp: 0 });
});

test("changes a band's type", async () => {
  const { screen, setState } = await renderEqualizer([low, high]);

  await screen.getByRole("combobox", { name: "Type of band 2" }).click();
  await screen.getByRole("option", { name: "High shelf" }).click();

  expect(setState).toHaveBeenCalledWith({ bands: [low, { ...high, type: "HighShelf" }], preamp: 0 });
});

test("disables the gain of low-pass and high-pass bands", async () => {
  const { screen } = await renderEqualizer([{ ...low, type: "HighPass" }, high]);

  const gains = screen.getByRole("slider", { name: "Gain" });
  await expect.element(gains.nth(0)).toHaveAttribute("aria-disabled", "true");
  await expect.element(gains.nth(1)).not.toHaveAttribute("aria-disabled");
});

test("changes the preamp", async () => {
  const { screen, setState } = await renderEqualizer([low]);

  screen.getByRole("slider", { name: "Preamp" }).element().focus();
  await userEvent.keyboard("{ArrowLeft}");

  expect(setState).toHaveBeenLastCalledWith({ bands: [low], preamp: expect.closeTo(-0.1, 6) as number });
});

test("imports an Equalizer APO file", async () => {
  const { screen, setState } = await renderEqualizer([low]);
  const file = new File(
    ["Preamp: -5 dB\nFilter 1: ON LSC Fc 105 Hz Gain 5 dB Q 0.7\nFilter 2: ON BP Fc 1000 Hz"],
    "ParametricEQ.txt",
    { type: "text/plain" }
  );

  await userEvent.upload(screen.getByLabelText("Import…", { exact: true }).last(), file);

  await expect.poll(() => setState.mock.calls.length).toBe(1);
  expect(setState).toHaveBeenCalledWith({
    bands: [{ type: "LowShelf", frequency: 105, gain: 5, q: 0.7 }],
    preamp: -5,
  });
  await expect
    .element(screen.getByText("Imported 1 band. Left out (other filter types or single channels): 1", { exact: false }))
    .toBeVisible();
});

test("reports a file without filters", async () => {
  const { screen, setState } = await renderEqualizer([low]);

  await userEvent.upload(
    screen.getByLabelText("Import…", { exact: true }).last(),
    new File(["hello"], "notes.txt", { type: "text/plain" })
  );

  await expect.element(screen.getByText("The file has no filters in the Equalizer APO format.")).toBeVisible();
  expect(setState).not.toHaveBeenCalled();
});

test("describes the import in a tooltip", async () => {
  const { screen } = await renderEqualizer([low]);

  await screen.getByRole("button", { name: "Import…" }).hover();

  await expect
    .element(screen.getByRole("button", { name: "Import…" }))
    .toHaveAccessibleDescription(/Equalizer APO configuration/);
});

test("has at most 32 bands", async () => {
  const { screen } = await renderEqualizer(Array.from({ length: 32 }, () => low));

  await expect.element(screen.getByRole("button", { name: "Add band" })).toBeDisabled();
});
