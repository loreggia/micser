import {
  Button,
  Caption1,
  Card,
  CardHeader,
  Menu,
  MenuItem,
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
import {
  ChevronDownRegular,
  ChevronUpRegular,
  DeleteRegular,
  DesktopSpeakerRegular,
  FlashOffRegular,
  MoreHorizontalRegular,
  Speaker2Regular,
  SpeakerMuteRegular,
} from "@fluentui/react-icons";
import {
  useModuleUpdate,
  type ModuleDto,
  type ModuleTypeDto,
  type SubgraphDto,
  type WidgetDefinition,
} from "@micser/web-sdk";
import { Handle, Position, useReactFlow, type Node, type NodeProps } from "@xyflow/react";
import { LevelMeter } from "./LevelMeter";
import { ModuleTitle } from "./ModuleTitle";

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
    width: "3em",
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
  portLabel: {
    position: "absolute",
    top: "-1.4em",
    whiteSpace: "nowrap",
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase100,
  },
});

/**
 * A module on the graph: name, mute, bypass, collapse and a menu (delete), volume (or the Windows volume), level meter, the module type's
 * widget, and the connectors. Collapsed, it shows only the name, mute, bypass and the connectors. While its subgraph is muted or bypassed, the module's
 * own switch shows that and is disabled.
 */
export function ModuleNode({ id, data, selected }: NodeProps<ModuleNodeType>) {
  const styles = useStyles();
  const update = useModuleUpdate();
  const { deleteElements } = useReactFlow();
  const { module, moduleType, widget, subgraph } = data;
  const mutedBySubgraph = subgraph?.isMuted === true;
  const bypassedBySubgraph = subgraph?.isBypassed === true;
  const Widget = widget?.component;
  const collapsed = module.isCollapsed;
  // collapsed, the card is only as high as its header, which would crowd several ports and their labels
  const portCount = Math.max(moduleType?.inputs.length ?? 0, moduleType?.outputs.length ?? 0);

  return (
    <Card
      className={mergeClasses(styles.card, selected && styles.selected)}
      style={collapsed && portCount > 1 ? { minHeight: `${(portCount + 1) * 24}px` } : undefined}
      size="small"
    >
      <CardHeader
        className={styles.header}
        header={
          <ModuleTitle
            name={module.name ?? null}
            fallback={widget?.title || module.type}
            label="Module name"
            onRename={(name) => update({ ...module, name } as ModuleDto)}
          />
        }
        description={module.name && widget && !collapsed ? <Caption1>{widget.title}</Caption1> : undefined}
        action={
          <div className={mergeClasses(styles.actions, "nodrag")}>
            {moduleType?.supportsBypass && (
              <Tooltip
                content={bypassedBySubgraph ? "Bypassed by subgraph" : module.isBypassed ? "Bypassed" : "Bypass"}
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
                  ? "Muted by subgraph"
                  : module.useSystemVolume
                    ? "Muted with Windows"
                    : module.isMuted
                      ? "Unmute"
                      : "Mute"
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
            <Tooltip content={collapsed ? "Expand" : "Collapse"} relationship="label">
              <Button
                size="small"
                appearance="subtle"
                icon={collapsed ? <ChevronDownRegular /> : <ChevronUpRegular />}
                onClick={() => update({ ...module, isCollapsed: !collapsed })}
              />
            </Tooltip>
            <Menu>
              <MenuTrigger disableButtonEnhancement>
                <Tooltip content="More" relationship="label">
                  <Button size="small" appearance="subtle" icon={<MoreHorizontalRegular />} />
                </Tooltip>
              </MenuTrigger>
              <MenuPopover>
                <MenuList>
                  <MenuItem icon={<DeleteRegular />} onClick={() => void deleteElements({ nodes: [{ id }] })}>
                    Delete
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
            <Caption1>Volume</Caption1>
            <Slider
              size="small"
              min={0}
              max={100}
              value={Math.round(module.volume * 100)}
              aria-label="Volume"
              disabled={module.useSystemVolume}
              onChange={(_, value) => update({ ...module, volume: value.value / 100 })}
            />
            <Caption1 className={styles.volumeValue}>{Math.round(module.volume * 100)}%</Caption1>
            <Tooltip
              content={module.useSystemVolume ? "Follows the Windows volume" : "Follow the Windows volume"}
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
      {moduleType?.inputs.map((port, index, ports) => (
        <Port
          key={port}
          type="target"
          id={port}
          label={ports.length > 1 ? port : undefined}
          index={index}
          count={ports.length}
        />
      ))}
      {moduleType?.outputs.map((port, index, ports) => (
        <Port
          key={port}
          type="source"
          id={port}
          label={ports.length > 1 ? port : undefined}
          index={index}
          count={ports.length}
        />
      ))}
    </Card>
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
