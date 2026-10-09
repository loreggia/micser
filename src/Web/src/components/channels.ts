import {
  formatNumber,
  type ConnectionDto,
  type ModuleDto,
  type ModulePortLayoutsDto,
  type ModuleTypeDto,
  type SpeakerPosition,
} from "@micser/web-sdk";
import type { useTranslation } from "../i18n";

type Translate = ReturnType<typeof useTranslation>["t"];

/** The channel counts the "Channels" menu offers besides Auto. */
export const channelCountPresets = [1, 2, 4, 6, 8] as const;

/** The highest channel count of a module (the engine's AudioModule.MaxChannelCount). */
export const maxChannelCount = 64;

/** A channel connector of a port. */
export interface PortChannel {
  /** 0-based. */
  index: number;
  /** The speaker of the channel, if the port's layout has speaker positions. */
  speaker?: SpeakerPosition;
}

/**
 * The channel connectors of a port: its channels in the last processed block, at least as many as the module's channel count or, on
 * Auto, stereo for inputs that take their layout from their sources (which the engine widens to the channels connected to), and every
 * channel that a connection uses, also one the port doesn't have right now.
 */
export function portChannels(
  direction: "in" | "out",
  port: string,
  module: ModuleDto,
  moduleType: ModuleTypeDto | undefined,
  layouts: ModulePortLayoutsDto | undefined,
  connections: readonly ConnectionDto[]
): PortChannel[] {
  const layout = (direction === "in" ? layouts?.inputs : layouts?.outputs)?.[port];
  let count = layout?.channelCount ?? 0;
  if (moduleType?.supportsChannelCount) {
    count = Math.max(count, module.channelCount ?? (direction === "in" ? 2 : 0));
  }

  for (const connection of connections) {
    const channel =
      direction === "in"
        ? connection.targetModuleId === module.id && connection.targetPort === port && connection.targetChannel
        : connection.sourceModuleId === module.id && connection.sourcePort === port && connection.sourceChannel;
    if (typeof channel === "number") {
      count = Math.max(count, channel + 1);
    }
  }

  return Array.from({ length: count }, (_, index) => ({ index, speaker: layout?.speakers?.[index] }));
}

/** Whether connections take single channels of the module's outputs or add to single channels of its inputs. */
export function hasChannelConnections(moduleId: string, connections: readonly ConnectionDto[]) {
  return connections.some(
    (c) =>
      (c.sourceModuleId === moduleId && c.sourceChannel != null) ||
      (c.targetModuleId === moduleId && c.targetChannel != null)
  );
}

/** The lowest channel count that the connections to single channels of the module's inputs need; 0 without any. */
export function requiredChannelCount(moduleId: string, connections: readonly ConnectionDto[]) {
  return Math.max(
    0,
    ...connections
      .filter((c) => c.targetModuleId === moduleId && c.targetChannel != null)
      .map((c) => c.targetChannel! + 1)
  );
}

/**
 * Whether a connection takes or adds to a channel that its port doesn't have right now (e.g. a device with fewer channels, or none
 * while it's unplugged), so it's silent. Unknown while the layouts aren't loaded.
 */
export function isBeyondLayout(connection: ConnectionDto, layouts: readonly ModulePortLayoutsDto[] | undefined) {
  const count = (moduleId: string, direction: "in" | "out", port: string) => {
    const module = layouts?.find((l) => l.moduleId === moduleId);
    return (direction === "in" ? module?.inputs : module?.outputs)?.[port]?.channelCount;
  };
  const sourceCount = count(connection.sourceModuleId, "out", connection.sourcePort);
  const targetCount = count(connection.targetModuleId, "in", connection.targetPort);
  return (
    (connection.sourceChannel != null && sourceCount !== undefined && connection.sourceChannel >= sourceCount) ||
    (connection.targetChannel != null && targetCount !== undefined && connection.targetChannel >= targetCount)
  );
}

/** The name of a channel count in the "Channels" menu, e.g. "Stereo" or "12 channels". */
export function channelCountName(count: number, t: Translate) {
  switch (count) {
    case 1:
      return t("channels.mono");
    case 2:
      return t("channels.stereo");
    case 4:
      return t("channels.quad");
    case 6:
      return t("channels.surround51");
    case 8:
      return t("channels.surround71");
    default:
      return t("channels.countName", { count });
  }
}

/** A channel's label: its number and, if the layout has speaker positions, its speaker, e.g. "1 (L)". */
export function channelLabel(channel: PortChannel, t: Translate) {
  const number = formatNumber(channel.index + 1);
  return channel.speaker ? t("channels.label", { number, speaker: t(`speakers.${channel.speaker}`) }) : number;
}
