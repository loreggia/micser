import type { ConnectionDto, ModulePortLayoutsDto, ModuleTypeDto } from "@micser/web-sdk";
import { testModule } from "@micser/web-sdk/testing";
import { describe, expect, test } from "vitest";
import { hasChannelConnections, isBeyondLayout, portChannels, requiredChannelCount } from "../channels";

const gainType: ModuleTypeDto = {
  type: "Gain",
  inputs: ["Input"],
  outputs: ["Output"],
  defaultState: {},
  supportsBypass: true,
  supportsChannelCount: true,
};
const deviceType: ModuleTypeDto = { ...gainType, type: "DeviceOutput", outputs: [], supportsChannelCount: false };
const gain = testModule("Gain", { gain: 0 });
const device = testModule("DeviceOutput", { deviceId: null, adapterName: null }, { id: "device-1" });

function connection(overrides: Partial<ConnectionDto>): ConnectionDto {
  return {
    id: "connection-1",
    sourceModuleId: gain.id,
    sourcePort: "Output",
    targetModuleId: device.id,
    targetPort: "Input",
    ...overrides,
  };
}

const stereo: ModulePortLayoutsDto = {
  moduleId: gain.id,
  inputs: { Input: { channelCount: 2, speakers: ["FrontLeft", "FrontRight"] } },
  outputs: { Output: { channelCount: 2, speakers: ["FrontLeft", "FrontRight"] } },
};

describe("portChannels", () => {
  test("follow the port's layout with its speakers", () => {
    expect(portChannels("out", "Output", gain, gainType, stereo, [])).toEqual([
      { index: 0, speaker: "FrontLeft" },
      { index: 1, speaker: "FrontRight" },
    ]);
  });

  test("are at least stereo for an input on Auto and the channel count otherwise", () => {
    expect(portChannels("in", "Input", gain, gainType, undefined, [])).toHaveLength(2);
    expect(portChannels("out", "Output", gain, gainType, undefined, [])).toHaveLength(0);
    expect(portChannels("out", "Output", { ...gain, channelCount: 6 }, gainType, undefined, [])).toHaveLength(6);
  });

  test("include the channels connections use beyond the layout", () => {
    const layouts = { moduleId: device.id, inputs: { Input: { channelCount: 0, speakers: null } }, outputs: {} };

    const channels = portChannels("in", "Input", device, deviceType, layouts, [connection({ targetChannel: 3 })]);

    expect(channels.map((c) => c.index)).toEqual([0, 1, 2, 3]);
    expect(channels[3].speaker).toBeUndefined();
  });
});

test("hasChannelConnections counts the end that names a channel", () => {
  const toChannel = [connection({ targetChannel: 0 })];

  expect(hasChannelConnections(device.id, toChannel)).toBe(true);
  expect(hasChannelConnections(gain.id, toChannel)).toBe(false);
  expect(hasChannelConnections(gain.id, [connection({ sourceChannel: 1 })])).toBe(true);
});

test("requiredChannelCount is the highest target channel plus one", () => {
  expect(requiredChannelCount(device.id, [])).toBe(0);
  expect(requiredChannelCount(device.id, [connection({ targetChannel: 1 }), connection({ targetChannel: 5 })])).toBe(6);
});

test("isBeyondLayout marks channels the ports don't have right now", () => {
  const layouts: ModulePortLayoutsDto[] = [
    stereo,
    { moduleId: device.id, inputs: { Input: { channelCount: 2, speakers: null } }, outputs: {} },
  ];

  expect(isBeyondLayout(connection({ sourceChannel: 1, targetChannel: 1 }), layouts)).toBe(false);
  expect(isBeyondLayout(connection({ sourceChannel: 2 }), layouts)).toBe(true);
  expect(isBeyondLayout(connection({ targetChannel: 7 }), layouts)).toBe(true);
  expect(isBeyondLayout(connection({ targetChannel: 7 }), undefined)).toBe(false);
});
