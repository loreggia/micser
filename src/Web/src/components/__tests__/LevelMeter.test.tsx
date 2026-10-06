import { EngineConnection, type ModuleLevels } from "@micser/web-sdk";
import { createTestQueryClient, TestProviders } from "@micser/web-sdk/testing";
import { expect, test, vi } from "vitest";
import { render } from "vitest-browser-react";
import { LevelMeter } from "../LevelMeter";

async function renderMeter(levels: ModuleLevels | undefined) {
  const queryClient = createTestQueryClient();
  const connection = new EngineConnection(queryClient);
  vi.spyOn(connection, "levels", "get").mockReturnValue(levels);
  const unsubscribe = vi.fn();
  vi.spyOn(connection, "subscribeLevels").mockReturnValue(unsubscribe);

  const screen = await render(
    <TestProviders queryClient={queryClient} connection={connection}>
      <LevelMeter moduleId="gain-1" />
    </TestProviders>
  );

  return { screen, unsubscribe };
}

test("shows a meter per channel with its peak level", async () => {
  const { screen } = await renderMeter({ "gain-1": [{ port: "Output", peak: [1, 0.1], rms: [0.5, 0.05] }] });

  await expect.element(screen.getByRole("meter", { name: "Channel 1 level" })).toHaveAttribute("aria-valuenow", "0");
  await expect.element(screen.getByRole("meter", { name: "Channel 2 level" })).toHaveAttribute("aria-valuenow", "-20");
});

test("labels the ports of a module with several outputs", async () => {
  const { screen } = await renderMeter({
    "gain-1": [
      { port: "Left", peak: [0.5], rms: [0.2] },
      { port: "Right", peak: [0.5], rms: [0.2] },
    ],
  });

  await expect.element(screen.getByText("Left")).toBeVisible();
  await expect.element(screen.getByText("Right")).toBeVisible();
});

test("shows nothing before the module was processed, and unsubscribes when removed", async () => {
  const { screen, unsubscribe } = await renderMeter({ "other-module": [{ port: null, peak: [1], rms: [1] }] });

  expect(screen.container.querySelector("[role=meter]")).toBeNull();

  await screen.unmount();
  expect(unsubscribe).toHaveBeenCalled();
});
