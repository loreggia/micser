import {
  Button,
  Card,
  CardHeader,
  Menu,
  MenuItemRadio,
  MenuList,
  MenuPopover,
  MenuTrigger,
  ToggleButton,
  Tooltip,
  makeStyles,
  mergeClasses,
  tokens,
} from "@fluentui/react-components";
import {
  ArrowMinimizeRegular,
  ChevronDownRegular,
  ChevronUpRegular,
  CircleFilled,
  ColorRegular,
  FlashOffRegular,
  GroupDismissRegular,
  Speaker2Regular,
  SpeakerMuteRegular,
} from "@fluentui/react-icons";
import {
  getDeleteSubgraphMutationOptions,
  SubgraphColor,
  useGetModules,
  useModuleUpdate,
  useSubgraphUpdate,
  type ModuleDto,
  type SubgraphDto,
} from "@micser/web-sdk";
import { useMutation } from "@tanstack/react-query";
import { NodeResizeControl, useReactFlow, type Node, type NodeProps } from "@xyflow/react";
import { useNotifyError } from "../notifications";
import { Port } from "./ModuleNode";
import { ModuleTitle } from "./ModuleTitle";
import { frameAround, minSubgraphSize, type ProxyPort } from "./subgraphs";

export type SubgraphNodeData = {
  subgraph: SubgraphDto;
  /** The ports of connections from outside into the subgraph; shown while it's collapsed. */
  inputs: ProxyPort[];
  /** The ports of connections from the subgraph to the outside; shown while it's collapsed. */
  outputs: ProxyPort[];
};

export type SubgraphNodeType = Node<SubgraphNodeData, "subgraph">;

/** The theme colors of each subgraph color: the frame's border and its fill. */
const palette: Record<SubgraphColor, { border: string; fill: string }> = {
  Neutral: { border: tokens.colorNeutralStroke1, fill: tokens.colorNeutralBackground3 },
  Blue: { border: tokens.colorPaletteBlueBorderActive, fill: tokens.colorPaletteBlueBackground2 },
  Green: { border: tokens.colorPaletteGreenBorderActive, fill: tokens.colorPaletteGreenBackground2 },
  Yellow: { border: tokens.colorPaletteYellowBorderActive, fill: tokens.colorPaletteYellowBackground2 },
  Orange: { border: tokens.colorPaletteDarkOrangeBorderActive, fill: tokens.colorPaletteDarkOrangeBackground2 },
  Red: { border: tokens.colorPaletteRedBorderActive, fill: tokens.colorPaletteRedBackground2 },
  Purple: { border: tokens.colorPalettePurpleBorderActive, fill: tokens.colorPalettePurpleBackground2 },
  Teal: { border: tokens.colorPaletteTealBorderActive, fill: tokens.colorPaletteTealBackground2 },
};

const useStyles = makeStyles({
  frame: {
    boxSizing: "border-box",
    width: "100%",
    height: "100%",
    borderRadius: tokens.borderRadiusLarge,
  },
  frameHeader: {
    display: "flex",
    alignItems: "center",
    gap: tokens.spacingHorizontalS,
    paddingBlock: tokens.spacingVerticalXXS,
    paddingInline: tokens.spacingHorizontalS,
    borderTopLeftRadius: tokens.borderRadiusLarge,
    borderTopRightRadius: tokens.borderRadiusLarge,
    cursor: "grab",
  },
  title: {
    flexGrow: 1,
    minWidth: 0,
  },
  card: {
    minWidth: "240px",
    boxShadow: tokens.shadow8,
    overflow: "visible",
    borderTopWidth: tokens.strokeWidthThickest,
    borderTopStyle: "solid",
  },
  // full width, so the connectors sit on the card's edges
  proxyRow: {
    position: "relative",
    display: "flex",
    justifyContent: "space-between",
    alignItems: "center",
    gap: tokens.spacingHorizontalL,
    minHeight: "24px",
    marginInline: "calc(-1 * var(--fui-Card--size))",
    paddingInline: "var(--fui-Card--size)",
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
  },
  header: {
    cursor: "grab",
  },
  selected: {
    outline: `${tokens.strokeWidthThick} solid ${tokens.colorBrandStroke1}`,
  },
  actions: {
    display: "flex",
  },
});

// React Flow's handle styles outrank a class; the corner sits inside the frame
const resizeHandleStyle = {
  width: "12px",
  height: "12px",
  translate: "-100% -100%",
  backgroundColor: "transparent",
  border: "none",
  borderRadius: 0,
  borderRight: `${tokens.strokeWidthThick} solid ${tokens.colorNeutralStroke1}`,
  borderBottom: `${tokens.strokeWidthThick} solid ${tokens.colorNeutralStroke1}`,
};

/**
 * A subgraph on the graph. Expanded, it's a frame in its color behind its modules, with the name, color, fit to the modules, mute,
 * bypass, ungroup and collapse in its header and a resize handle at the bottom right. Collapsed, it's a node with a connector for each port that connections
 * from or to the outside use.
 */
export function SubgraphNode({ data, selected }: NodeProps<SubgraphNodeType>) {
  const styles = useStyles();
  const update = useSubgraphUpdate();
  const updateModule = useModuleUpdate();
  const { data: modules } = useGetModules();
  const { getNodes } = useReactFlow();
  const notifyError = useNotifyError();
  const ungroup = useMutation({
    ...getDeleteSubgraphMutationOptions(),
    onError: (error) => notifyError("Ungrouping failed", error),
  });
  const { subgraph, inputs, outputs } = data;
  const colors = palette[subgraph.color];
  const members = modules?.filter((m) => m.subgraphId === subgraph.id) ?? [];

  // the frame around the modules as they are measured; their positions move by as much as the frame, so they stay in place
  const fitToModules = () => {
    const sizes = new Map(getNodes().map((node) => [node.id, node.measured]));
    const rects = members.map((module) => {
      const position = module.position ?? { x: 0, y: 0 };
      const size = sizes.get(module.id);
      return {
        x: position.x,
        y: position.y,
        right: position.x + (size?.width ?? 0),
        bottom: position.y + (size?.height ?? 0),
      };
    });
    if (rects.length === 0) {
      return;
    }

    const left = Math.min(...rects.map((r) => r.x));
    const top = Math.min(...rects.map((r) => r.y));
    const frame = frameAround({
      x: subgraph.position.x + left,
      y: subgraph.position.y + top,
      width: Math.max(...rects.map((r) => r.right)) - left,
      height: Math.max(...rects.map((r) => r.bottom)) - top,
    });
    const dx = frame.position.x - subgraph.position.x;
    const dy = frame.position.y - subgraph.position.y;
    update({ ...subgraph, ...frame });
    for (const module of members) {
      const position = module.position ?? { x: 0, y: 0 };
      updateModule({ ...module, position: { x: position.x - dx, y: position.y - dy } } as ModuleDto);
    }
  };

  const title = (
    <ModuleTitle
      name={subgraph.name}
      fallback="Subgraph"
      label="Subgraph name"
      onRename={(name) => update({ ...subgraph, name })}
    />
  );

  const actions = (
    <div className={mergeClasses(styles.actions, "nodrag")}>
      {!subgraph.isCollapsed && (
        <Menu
          checkedValues={{ color: [subgraph.color] }}
          onCheckedValueChange={(_, { checkedItems }) =>
            update({ ...subgraph, color: checkedItems[0] as SubgraphColor })
          }
        >
          <MenuTrigger disableButtonEnhancement>
            <Tooltip content="Color" relationship="label">
              <Button size="small" appearance="subtle" icon={<ColorRegular />} />
            </Tooltip>
          </MenuTrigger>
          <MenuPopover>
            <MenuList>
              {Object.values(SubgraphColor).map((color) => (
                <MenuItemRadio
                  key={color}
                  name="color"
                  value={color}
                  icon={<CircleFilled style={{ color: palette[color].border }} />}
                >
                  {color}
                </MenuItemRadio>
              ))}
            </MenuList>
          </MenuPopover>
        </Menu>
      )}
      {!subgraph.isCollapsed && (
        <Tooltip content="Fit to modules" relationship="label">
          <Button
            size="small"
            appearance="subtle"
            icon={<ArrowMinimizeRegular />}
            disabled={members.length === 0}
            onClick={fitToModules}
          />
        </Tooltip>
      )}
      <Tooltip content={subgraph.isBypassed ? "Bypassed" : "Bypass all"} relationship="label">
        <ToggleButton
          size="small"
          appearance="subtle"
          checked={subgraph.isBypassed}
          icon={<FlashOffRegular />}
          onClick={() => update({ ...subgraph, isBypassed: !subgraph.isBypassed })}
        />
      </Tooltip>
      <Tooltip content={subgraph.isMuted ? "Unmute" : "Mute all"} relationship="label">
        <ToggleButton
          size="small"
          appearance="subtle"
          checked={subgraph.isMuted}
          icon={subgraph.isMuted ? <SpeakerMuteRegular /> : <Speaker2Regular />}
          onClick={() => update({ ...subgraph, isMuted: !subgraph.isMuted })}
        />
      </Tooltip>
      {!subgraph.isCollapsed && (
        <Tooltip content="Ungroup" relationship="label">
          <Button
            size="small"
            appearance="subtle"
            icon={<GroupDismissRegular />}
            onClick={() => ungroup.mutate({ id: subgraph.id })}
          />
        </Tooltip>
      )}
      <Tooltip content={subgraph.isCollapsed ? "Expand" : "Collapse"} relationship="label">
        <Button
          size="small"
          appearance="subtle"
          icon={subgraph.isCollapsed ? <ChevronDownRegular /> : <ChevronUpRegular />}
          onClick={() => update({ ...subgraph, isCollapsed: !subgraph.isCollapsed })}
        />
      </Tooltip>
    </div>
  );

  if (subgraph.isCollapsed) {
    const rows = Array.from({ length: Math.max(inputs.length, outputs.length) }, (_, index) => ({
      input: inputs.at(index),
      output: outputs.at(index),
    }));
    return (
      <Card
        className={mergeClasses(styles.card, selected && styles.selected)}
        style={{ borderTopColor: colors.border }}
        size="small"
      >
        <CardHeader className={styles.header} header={title} action={actions} />
        {rows.map(({ input, output }) => (
          <div key={`${input?.id}|${output?.id}`} className={styles.proxyRow}>
            <span>{input?.label}</span>
            <span>{output?.label}</span>
            {input && <Port type="target" id={input.id} index={0} count={1} />}
            {output && <Port type="source" id={output.id} index={0} count={1} />}
          </div>
        ))}
      </Card>
    );
  }

  return (
    <div
      className={mergeClasses(styles.frame, selected && styles.selected)}
      style={{
        border: `${tokens.strokeWidthThick} solid ${colors.border}`,
        backgroundColor: `color-mix(in srgb, ${colors.fill} 35%, transparent)`,
      }}
    >
      <div className={styles.frameHeader} style={{ backgroundColor: colors.fill }}>
        <div className={styles.title}>{title}</div>
        {actions}
      </div>
      <NodeResizeControl
        position="bottom-right"
        style={resizeHandleStyle}
        minWidth={minSubgraphSize.width}
        minHeight={minSubgraphSize.height}
        onResizeEnd={(_, size) =>
          update({ ...subgraph, size: { width: Math.round(size.width), height: Math.round(size.height) } })
        }
      />
    </div>
  );
}
