import {
  Button,
  Caption1,
  Card,
  CardHeader,
  Menu,
  MenuDivider,
  MenuItem,
  MenuItemCheckbox,
  MenuItemRadio,
  MenuList,
  MenuPopover,
  MenuTrigger,
  Slider,
  ToggleButton,
  Tooltip,
  makeStyles,
  mergeClasses,
  tokens,
} from "@fluentui/react-components";
import { ChevronDownRegular } from "@fluentui/react-icons/svg/chevron-down";
import { ChevronUpRegular } from "@fluentui/react-icons/svg/chevron-up";
import { DeleteRegular } from "@fluentui/react-icons/svg/delete";
import { DesktopSpeakerRegular } from "@fluentui/react-icons/svg/desktop-speaker";
import { FlashOffRegular } from "@fluentui/react-icons/svg/flash-off";
import { MoreHorizontalRegular } from "@fluentui/react-icons/svg/more-horizontal";
import { Speaker2Regular } from "@fluentui/react-icons/svg/speaker";
import { SpeakerMuteRegular } from "@fluentui/react-icons/svg/speaker-mute";
import {
  formatNumber,
  localize,
  useGetConnections,
  useModuleUpdate,
  usePortLayouts,
  type ConnectionDto,
  type ModuleDto,
  type ModuleTypeDto,
  type SubgraphDto,
  type WidgetDefinition,
} from "@micser/web-sdk";
import { Handle, Position, useReactFlow, type Node, type NodeProps } from "@xyflow/react";
import { useTranslation } from "../i18n";
import { portName } from "../plugins";
import {
  channelCountName,
  channelCountPresets,
  channelLabel,
  hasChannelConnections,
  portChannels,
  requiredChannelCount,
  type PortChannel,
} from "./channels";
import { LevelMeter } from "./LevelMeter";
import { useModuleActions } from "./moduleActions";
import { ModuleTitle } from "./ModuleTitle";
import { portHandleId } from "./subgraphs";
import { useHandlesChanged } from "./useHandlesChanged";

export type ModuleNodeData = {
  module: ModuleDto;
  moduleType?: ModuleTypeDto;
  widget?: WidgetDefinition;
  /** The subgraph the module belongs to, whose mute and bypass apply to it too. */
  subgraph?: SubgraphDto;
};

export type ModuleNodeType = Node<ModuleNodeData, "module">;

const useStyles = makeStyles({
  card: {
    minWidth: "240px",
    boxShadow: tokens.shadow8,
    // the ports sit on the card's edges and would be cut in half
    overflow: "visible",
  },
  selected: {
    outline: `${tokens.strokeWidthThick} solid ${tokens.colorBrandStroke1}`,
  },
  header: {
    cursor: "grab",
  },
  actions: {
    display: "flex",
  },
  volume: {
    display: "grid",
    gridTemplateColumns: "auto 1fr auto auto",
    alignItems: "center",
    gap: tokens.spacingHorizontalXS,
  },
  volumeValue: {
    width: "3.5em",
    textAlign: "right",
    color: tokens.colorNeutralForeground3,
    fontVariantNumeric: "tabular-nums",
  },
  body: {
    cursor: "default",
  },
  port: {
    width: "12px",
    height: "12px",
    border: `${tokens.strokeWidthThick} solid ${tokens.colorNeutralBackground1}`,
    backgroundColor: tokens.colorBrandBackground,
  },
  channels: {
    marginTop: tokens.spacingVerticalS,
    paddingTop: tokens.spacingVerticalXS,
    borderTop: `${tokens.strokeWidthThin} solid ${tokens.colorNeutralStroke2}`,
  },
  // full width, so the connectors sit on the card's edges
  channelRow: {
    position: "relative",
    display: "flex",
    justifyContent: "space-between",
    alignItems: "center",
    gap: tokens.spacingHorizontalL,
    minHeight: "20px",
    marginInline: "calc(-1 * var(--fui-Card--size))",
    paddingInline: "var(--fui-Card--size)",
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
  },
  portRowLabel: {
    color: tokens.colorNeutralForeground2,
    fontWeight: tokens.fontWeightSemibold,
  },
  portLabel: {
    position: "absolute",
    top: "-1.4em",
    whiteSpace: "nowrap",
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase100,
  },
});

/**
 * A module on the graph: name, mute, bypass, collapse and a menu (channels, delete), volume (or the Windows volume), level meter, the
 * module type's widget, and the connectors. Collapsed, it shows only the name, mute, bypass and the connectors. While its subgraph is
 * muted or bypassed, the module's own switch shows that and is disabled.
 *
 * With its channels shown, each port has a row for the whole port and one per channel, each with its connector; "Show channels" can't
 * be turned off while connections use single channels of the module.
 */
export function ModuleNode({ id, data, selected }: NodeProps<ModuleNodeType>) {
  const styles = useStyles();
  const { t } = useTranslation();
  const update = useModuleUpdate();
  const { deleteElements } = useReactFlow();
  const { module, moduleType, widget, subgraph } = data;
  const { data: connections = [] } = useGetConnections();
  const actions = useModuleActions();
  const channelsInUse = hasChannelConnections(module.id, connections);
  const requiredChannels = requiredChannelCount(module.id, connections);
  const channelCounts = [...channelCountPresets, ...(module.channelCount ? [module.channelCount] : [])]
    .filter((count, index, counts) => counts.indexOf(count) === index)
    .toSorted((a, b) => a - b);
  const mutedBySubgraph = subgraph?.isMuted === true;
  const bypassedBySubgraph = subgraph?.isBypassed === true;
  const Widget = widget?.component;
  const collapsed = module.isCollapsed;
  // collapsed, the card is only as high as its header, which would crowd several ports and their labels
  const portCount = Math.max(moduleType?.inputs.length ?? 0, moduleType?.outputs.length ?? 0);
  const widgetTitle = widget && localize(widget.title);
  const portLabel = (port: string, ports: string[]) => (ports.length > 1 ? portName(widget, port) : undefined);

  return (
    <Card
      className={mergeClasses(styles.card, selected && styles.selected)}
      style={
        collapsed && portCount > 1 && !module.showChannels ? { minHeight: `${(portCount + 1) * 24}px` } : undefined
      }
      size="small"
    >
      <CardHeader
        className={styles.header}
        header={
          <ModuleTitle
            name={module.name ?? null}
            fallback={widgetTitle || module.type}
            label={t("module.name")}
            onRename={(name) => update({ ...module, name } as ModuleDto)}
          />
        }
        description={module.name && widget && !collapsed ? <Caption1>{widgetTitle}</Caption1> : undefined}
        action={
          <div className={mergeClasses(styles.actions, "nodrag")}>
            {moduleType?.supportsBypass && (
              <Tooltip
                content={
                  bypassedBySubgraph
                    ? t("module.bypassedBySubgraph")
                    : module.isBypassed
                      ? t("common.bypassed")
                      : t("common.bypass")
                }
                relationship="label"
              >
                <ToggleButton
                  size="small"
                  appearance="subtle"
                  checked={module.isBypassed || bypassedBySubgraph}
                  disabled={bypassedBySubgraph}
                  icon={<FlashOffRegular />}
                  onClick={() => update({ ...module, isBypassed: !module.isBypassed })}
                />
              </Tooltip>
            )}
            <Tooltip
              content={
                mutedBySubgraph
                  ? t("module.mutedBySubgraph")
                  : module.useSystemVolume
                    ? t("module.mutedWithWindows")
                    : module.isMuted
                      ? t("common.unmute")
                      : t("common.mute")
              }
              relationship="label"
            >
              <ToggleButton
                size="small"
                appearance="subtle"
                checked={module.isMuted || mutedBySubgraph}
                disabled={module.useSystemVolume || mutedBySubgraph}
                icon={module.isMuted || mutedBySubgraph ? <SpeakerMuteRegular /> : <Speaker2Regular />}
                onClick={() => update({ ...module, isMuted: !module.isMuted })}
              />
            </Tooltip>
            <Tooltip content={collapsed ? t("common.expand") : t("common.collapse")} relationship="label">
              <Button
                size="small"
                appearance="subtle"
                icon={collapsed ? <ChevronDownRegular /> : <ChevronUpRegular />}
                onClick={() => update({ ...module, isCollapsed: !collapsed })}
              />
            </Tooltip>
            <Menu
              checkedValues={{ showChannels: module.showChannels ? ["shown"] : [] }}
              onCheckedValueChange={(_, { name, checkedItems }) => {
                if (name === "showChannels") {
                  update({ ...module, showChannels: checkedItems.includes("shown") });
                }
              }}
            >
              <MenuTrigger disableButtonEnhancement>
                <Tooltip content={t("common.more")} relationship="label">
                  <Button size="small" appearance="subtle" icon={<MoreHorizontalRegular />} />
                </Tooltip>
              </MenuTrigger>
              <MenuPopover>
                <MenuList>
                  {module.showChannels && channelsInUse ? (
                    <Tooltip content={t("channels.inUse")} relationship="description">
                      <MenuItemCheckbox name="showChannels" value="shown" disabled>
                        {t("channels.show")}
                      </MenuItemCheckbox>
                    </Tooltip>
                  ) : (
                    <MenuItemCheckbox name="showChannels" value="shown">
                      {t("channels.show")}
                    </MenuItemCheckbox>
                  )}
                  {moduleType?.supportsChannelCount && (
                    <Menu
                      checkedValues={{
                        channelCount: [module.channelCount == null ? "auto" : String(module.channelCount)],
                      }}
                      onCheckedValueChange={(_, { checkedItems }) =>
                        update({
                          ...module,
                          channelCount: checkedItems[0] === "auto" ? null : Number(checkedItems[0]),
                        } as ModuleDto)
                      }
                    >
                      <MenuTrigger disableButtonEnhancement>
                        <MenuItem>{t("channels.menu")}</MenuItem>
                      </MenuTrigger>
                      <MenuPopover>
                        <MenuList>
                          <MenuItemRadio name="channelCount" value="auto">
                            {t("channels.auto")}
                          </MenuItemRadio>
                          {channelCounts.map((count) => (
                            <MenuItemRadio
                              key={count}
                              name="channelCount"
                              value={String(count)}
                              disabled={count < requiredChannels}
                            >
                              {channelCountName(count, t)}
                            </MenuItemRadio>
                          ))}
                          <MenuDivider />
                          <MenuItem onClick={() => actions.chooseChannelCount(module)}>{t("channels.custom")}</MenuItem>
                        </MenuList>
                      </MenuPopover>
                    </Menu>
                  )}
                  <MenuDivider />
                  <MenuItem icon={<DeleteRegular />} onClick={() => void deleteElements({ nodes: [{ id }] })}>
                    {t("common.delete")}
                  </MenuItem>
                </MenuList>
              </MenuPopover>
            </Menu>
          </div>
        }
      />
      {!collapsed && (
        <>
          <div className={mergeClasses(styles.volume, "nodrag", "nowheel")}>
            <Caption1>{t("module.volume")}</Caption1>
            <Slider
              size="small"
              min={0}
              max={100}
              value={Math.round(module.volume * 100)}
              aria-label={t("module.volume")}
              disabled={module.useSystemVolume}
              onChange={(_, value) => update({ ...module, volume: value.value / 100 })}
            />
            <Caption1 className={styles.volumeValue}>
              {formatNumber(Math.round(module.volume * 100) / 100, { style: "percent" })}
            </Caption1>
            <Tooltip
              content={module.useSystemVolume ? t("module.followsWindowsVolume") : t("module.followWindowsVolume")}
              relationship="label"
            >
              <ToggleButton
                size="small"
                appearance="subtle"
                checked={module.useSystemVolume}
                icon={<DesktopSpeakerRegular />}
                onClick={() => update({ ...module, useSystemVolume: !module.useSystemVolume })}
              />
            </Tooltip>
          </div>
          <LevelMeter moduleId={module.id} />
          {Widget && (
            <div className={mergeClasses(styles.body, "nodrag", "nowheel")}>
              <Widget module={module} setState={(state) => update({ ...module, state } as ModuleDto)} />
            </div>
          )}
        </>
      )}
      {module.showChannels ? (
        <ChannelPorts module={module} moduleType={moduleType} widget={widget} connections={connections} />
      ) : (
        <>
          {moduleType?.inputs.map((port, index, ports) => (
            <Port
              key={port}
              type="target"
              id={port}
              label={portLabel(port, ports)}
              index={index}
              count={ports.length}
            />
          ))}
          {moduleType?.outputs.map((port, index, ports) => (
            <Port
              key={port}
              type="source"
              id={port}
              label={portLabel(port, ports)}
              index={index}
              count={ports.length}
            />
          ))}
        </>
      )}
    </Card>
  );
}

/** A port's row, or the row of one of its channels. */
interface PortRow {
  port: string;
  channel?: PortChannel;
}

/** The connectors of a module that shows its channels: per port, a row for the whole port and one per channel. */
function ChannelPorts({
  module,
  moduleType,
  widget,
  connections,
}: {
  module: ModuleDto;
  moduleType?: ModuleTypeDto;
  widget?: WidgetDefinition;
  connections: ConnectionDto[];
}) {
  const styles = useStyles();
  const { t } = useTranslation();
  const layouts = usePortLayouts(module.id);
  const rowsOf = (direction: "in" | "out", ports: string[] = []) =>
    ports.flatMap((port): PortRow[] => [
      { port },
      ...portChannels(direction, port, module, moduleType, layouts, connections).map((channel) => ({ port, channel })),
    ]);
  const inputs = rowsOf("in", moduleType?.inputs);
  const outputs = rowsOf("out", moduleType?.outputs);
  const label = (row: PortRow) =>
    row.channel ? (
      channelLabel(row.channel, t)
    ) : (
      <span className={styles.portRowLabel}>{portName(widget, row.port)}</span>
    );
  const handleId = (row: PortRow) => portHandleId(row.port, row.channel?.index);
  useHandlesChanged([...inputs.map((row) => `in|${handleId(row)}`), ...outputs.map((row) => `out|${handleId(row)}`)]);

  return (
    <div className={styles.channels}>
      {Array.from({ length: Math.max(inputs.length, outputs.length) }, (_, index) => {
        const input = inputs.at(index);
        const output = outputs.at(index);
        return (
          <div key={`${input && handleId(input)}|${output && handleId(output)}`} className={styles.channelRow}>
            <span>{input && label(input)}</span>
            <span>{output && label(output)}</span>
            {input && <Port type="target" id={handleId(input)} index={0} count={1} />}
            {output && <Port type="source" id={handleId(output)} index={0} count={1} />}
          </div>
        );
      })}
    </div>
  );
}

/** A connector on the edge of a node, spread evenly with the others on the same side. */
export function Port({
  type,
  id,
  label,
  index,
  count,
}: {
  type: "source" | "target";
  id: string;
  label?: string;
  index: number;
  count: number;
}) {
  const styles = useStyles();
  const top = `${((index + 1) / (count + 1)) * 100}%`;

  return (
    <Handle
      id={id}
      type={type}
      position={type === "target" ? Position.Left : Position.Right}
      className={styles.port}
      style={{ top }}
    >
      {label && (
        <span className={styles.portLabel} style={type === "target" ? { left: 0 } : { right: 0 }}>
          {label}
        </span>
      )}
    </Handle>
  );
}
