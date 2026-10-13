import { getGetPortLayoutsQueryKey, type DelayState, type ModulePortLayoutsDto } from "@micser/web-sdk";
import { createTestQueryClient, testModule, TestProviders } from "@micser/web-sdk/testing";
import { expect, test, vi } from "vitest";
import { userEvent } from "vitest/browser";
import { render } from "vitest-browser-react";
import { DelayWidget } from "../DelayWidget";

async function renderDelay(state: DelayState, channelCount = 2) {
  const setState = vi.fn();
  const queryClient = createTestQueryClient();
  const layouts: ModulePortLayoutsDto = {
    moduleId: "Delay-1",
    inputs: { Input: { channelCount, speakers: null } },
    outputs: { Output: { channelCount, speakers: null } },
  };
  queryClient.setQueryData(getGetPortLayoutsQueryKey(), [layouts]);
  const screen = await render(
    <TestProviders queryClient={queryClient}>
      <DelayWidget module={testModule("Delay", state)} setState={setState} />
    </TestProviders>
  );

  return { screen, setState };
}

test("shows the delay and hides the channels without channel delays", async () => {
  const { screen } = await renderDelay({ time: 0.0125, channels: [] });

  await expect.element(screen.getByText("12.5 ms", { exact: true })).toBeVisible();
  await expect.element(screen.getByRole("switch", { name: "Per channel" })).not.toBeChecked();
  expect(screen.getByRole("slider", { name: "Channel 1" }).elements()).toHaveLength(0);
});

test("changes the delay and keeps the channels", async () => {
  const channels = [{ time: 0.001 }];
  const { screen, setState } = await renderDelay({ time: 0.01, channels });

  screen.getByRole("slider", { name: "Delay" }).element().focus();
  await userEvent.keyboard("{ArrowRight}");

  expect(setState).toHaveBeenLastCalledWith({ time: 0.0101, channels });
});

test("turning on per channel adds a delay for each input channel", async () => {
  const { screen, setState } = await renderDelay({ time: 0, channels: [] }, 6);

  await screen.getByRole("switch", { name: "Per channel" }).click();

  expect(setState).toHaveBeenLastCalledWith({ time: 0, channels: Array.from({ length: 6 }, () => ({ time: 0 })) });
});

test("shows a slider per channel and changes one", async () => {
  const { screen, setState } = await renderDelay({ time: 0, channels: [{ time: 0.002 }] });

  await expect.element(screen.getByText("+2.0 ms", { exact: true })).toBeVisible();
  screen.getByRole("slider", { name: "Channel 2" }).element().focus();
  await userEvent.keyboard("{ArrowRight}");

  expect(setState).toHaveBeenLastCalledWith({ time: 0, channels: [{ time: 0.002 }, { time: 0.0001 }] });
});
