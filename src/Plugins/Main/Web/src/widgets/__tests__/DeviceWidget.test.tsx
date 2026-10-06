import {
  EngineConnection,
  getGetDevicesQueryKey,
  getGetPreferencesQueryKey,
  type AudioDeviceInfo,
  type DeviceInputState,
} from "@micser/web-sdk";
import { createTestQueryClient, testModule, TestProviders } from "@micser/web-sdk/testing";
import { expect, test, vi } from "vitest";
import { render } from "vitest-browser-react";
import { DeviceWidget } from "../DeviceWidget";

function device(id: string, name: string, isActive = true): AudioDeviceInfo {
  return {
    id,
    name,
    description: null,
    adapterName: `${name} adapter`,
    direction: "Input",
    isActive,
    layout: null,
    sampleRate: 48000,
  };
}

const devices = [device("mic-10", "Mic 10"), device("mic-2", "Mic 2"), device("old", "Old mic", false)];

async function renderDevice(state: Partial<DeviceInputState>, showStreamStatistics = false) {
  const queryClient = createTestQueryClient();
  queryClient.setQueryData(getGetDevicesQueryKey({ direction: "Input", includeInactive: true }), devices);
  queryClient.setQueryData(getGetPreferencesQueryKey(), { showStreamStatistics, snapToGrid: true });

  const connection = new EngineConnection(queryClient);
  const dataListeners: ((data: unknown) => void)[] = [];
  vi.spyOn(connection, "subscribe").mockImplementation((_, listener) => {
    dataListeners.push(listener);
    return () => {};
  });

  const setState = vi.fn();
  const module = testModule("DeviceInput", { deviceId: null, adapterName: null, ...state });
  const screen = await render(
    <TestProviders queryClient={queryClient} connection={connection}>
      <DeviceWidget module={module} setState={setState} direction="Input" />
    </TestProviders>
  );

  const pushData = (data: unknown) => dataListeners.forEach((listener) => listener(data));
  return { screen, setState, pushData, dropdown: screen.getByRole("combobox") };
}

test("shows the selected device", async () => {
  const { dropdown } = await renderDevice({ deviceId: "mic-2" });

  await expect.element(dropdown).toHaveTextContent("Mic 2");
});

test("shows when there is no device", async () => {
  const { dropdown } = await renderDevice({});

  await expect.element(dropdown).toHaveTextContent("No device");
});

test("shows a missing device with its adapter", async () => {
  const { dropdown } = await renderDevice({ deviceId: "unplugged", adapterName: "USB Audio" });

  await expect.element(dropdown).toHaveTextContent("Unavailable (USB Audio)");
});

test("lists the active devices in natural order", async () => {
  const { screen, dropdown } = await renderDevice({});

  await dropdown.click();

  await expect
    .poll(() =>
      screen
        .getByRole("option")
        .elements()
        .map((option) => option.textContent)
    )
    .toEqual(["No device", "Mic 2", "Mic 10"]);
});

test("lists the selected device while it's inactive, disabled", async () => {
  const { screen, dropdown } = await renderDevice({ deviceId: "old" });

  await dropdown.click();

  await expect
    .element(screen.getByRole("option", { name: "Old mic (unavailable)" }))
    .toHaveAttribute("aria-disabled", "true");
});

test("selecting a device resets the buffer", async () => {
  const { screen, dropdown, setState } = await renderDevice({ deviceId: "mic-10", bufferMilliseconds: 20 });

  await dropdown.click();
  await screen.getByRole("option", { name: "Mic 2" }).click();

  expect(setState).toHaveBeenCalledWith({ deviceId: "mic-2", adapterName: "Mic 2 adapter", bufferMilliseconds: null });
});

test("selecting no device clears it", async () => {
  const { screen, dropdown, setState } = await renderDevice({ deviceId: "mic-10" });

  await dropdown.click();
  await screen.getByRole("option", { name: "No device" }).click();

  expect(setState).toHaveBeenCalledWith({ deviceId: null, adapterName: null, bufferMilliseconds: null });
});

test("shows the stream statistics only when enabled", async () => {
  const { screen } = await renderDevice({ deviceId: "mic-2" });

  await expect.element(screen.getByRole("combobox")).toBeVisible();
  await expect.element(screen.getByText("Not running")).not.toBeInTheDocument();
});

test("shows the stream's dropouts and buffer", async () => {
  const { screen, pushData } = await renderDevice({ deviceId: "mic-2" }, true);

  await expect.element(screen.getByText("Not running")).toBeVisible();

  const statistics = { fill: 0, targetFill: 0, correction: 0, overruns: 0, resyncs: 0, targetMilliseconds: 12.34 };
  pushData({ ...statistics, underruns: 0 });
  await expect.element(screen.getByText("Running · 12.3 ms buffer")).toBeVisible();

  pushData({ ...statistics, underruns: 2, resyncs: 1 });
  await expect.element(screen.getByText("3 dropouts · 12.3 ms buffer")).toBeVisible();
});
