import {
  Body1Strong,
  Button,
  Caption1,
  Card,
  CardHeader,
  Input,
  Slider,
  ToggleButton,
  Tooltip,
  makeStyles,
  mergeClasses,
  tokens,
} from "@fluentui/react-components";
import { DeleteRegular, FlashOffRegular, Speaker2Regular, SpeakerMuteRegular } from "@fluentui/react-icons";
import { useModuleUpdate, type ModuleDto, type ModuleTypeDto, type WidgetDefinition } from "@micser/web-sdk";
import { Handle, Position, useReactFlow, type Node, type NodeProps } from "@xyflow/react";
import { useRef, useState } from "react";
import { LevelMeter } from "./LevelMeter";

export type ModuleNodeData = {
  module: ModuleDto;
  moduleType?: ModuleTypeDto;
  widget?: WidgetDefinition;
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
  title: {
    cursor: "text",
  },
  titleInput: {
    width: "100%",
  },
  actions: {
    display: "flex",
  },
  volume: {
    display: "grid",
    gridTemplateColumns: "auto 1fr auto",
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
 * A module on the graph: name, mute, bypass, volume, level meter, the module type's widget, and the connectors.
 */
export function ModuleNode({ id, data, selected }: NodeProps<ModuleNodeType>) {
  const styles = useStyles();
  const update = useModuleUpdate();
  const { deleteElements } = useReactFlow();
  const { module, moduleType, widget } = data;
  const Widget = widget?.component;

  return (
    <Card className={mergeClasses(styles.card, selected && styles.selected)} size="small">
      <CardHeader
        className={styles.header}
        header={
          <ModuleTitle
            name={module.name ?? null}
            typeTitle={widget?.title || module.type}
            onRename={(name) => update({ ...module, name } as ModuleDto)}
          />
        }
        description={module.name && widget ? <Caption1>{widget.title}</Caption1> : undefined}
        action={
          <div className={mergeClasses(styles.actions, "nodrag")}>
            {moduleType?.supportsBypass && (
              <Tooltip content={module.isBypassed ? "Bypassed" : "Bypass"} relationship="label">
                <ToggleButton
                  size="small"
                  appearance="subtle"
                  checked={module.isBypassed}
                  icon={<FlashOffRegular />}
                  onClick={() => update({ ...module, isBypassed: !module.isBypassed })}
                />
              </Tooltip>
            )}
            <Tooltip content={module.isMuted ? "Unmute" : "Mute"} relationship="label">
              <ToggleButton
                size="small"
                appearance="subtle"
                checked={module.isMuted}
                icon={module.isMuted ? <SpeakerMuteRegular /> : <Speaker2Regular />}
                onClick={() => update({ ...module, isMuted: !module.isMuted })}
              />
            </Tooltip>
            <Tooltip content="Remove" relationship="label">
              <Button
                size="small"
                appearance="subtle"
                icon={<DeleteRegular />}
                onClick={() => void deleteElements({ nodes: [{ id }] })}
              />
            </Tooltip>
          </div>
        }
      />
      <div className={mergeClasses(styles.volume, "nodrag", "nowheel")}>
        <Caption1>Volume</Caption1>
        <Slider
          size="small"
          min={0}
          max={100}
          value={Math.round(module.volume * 100)}
          aria-label="Volume"
          onChange={(_, value) => update({ ...module, volume: value.value / 100 })}
        />
        <Caption1 className={styles.volumeValue}>{Math.round(module.volume * 100)}%</Caption1>
      </div>
      <LevelMeter moduleId={module.id} />
      {Widget && (
        <div className={mergeClasses(styles.body, "nodrag", "nowheel")}>
          <Widget module={module} setState={(state) => update({ ...module, state } as ModuleDto)} />
        </div>
      )}
      {moduleType?.inputs.map((port, index, ports) => (
        <Ports key={port} type="target" port={port} index={index} count={ports.length} />
      ))}
      {moduleType?.outputs.map((port, index, ports) => (
        <Ports key={port} type="source" port={port} index={index} count={ports.length} />
      ))}
    </Card>
  );
}

/** Longest name the engine accepts. */
const maxNameLength = 100;

/**
 * The module's title. Double-click to rename: Enter or leaving the field saves, Escape cancels, and an empty name goes back to the
 * module type's title.
 */
function ModuleTitle({
  name,
  typeTitle,
  onRename,
}: {
  name: string | null;
  /** Shown without a name. */
  typeTitle: string;
  onRename: (name: string | null) => void;
}) {
  const styles = useStyles();
  const [draft, setDraft] = useState<string>();
  // the field may also lose focus when it's removed after Enter or Escape
  const isFinished = useRef(false);
  const title = name || typeTitle;

  if (draft === undefined) {
    return (
      <Tooltip content="Double-click to rename" relationship="description">
        <Body1Strong
          className={styles.title}
          onDoubleClick={() => {
            isFinished.current = false;
            setDraft(name ?? "");
          }}
        >
          {title}
        </Body1Strong>
      </Tooltip>
    );
  }

  const finish = () => {
    isFinished.current = true;
    setDraft(undefined);
  };

  const save = () => {
    if (isFinished.current) {
      return;
    }

    const renamed = draft.trim() || null;
    finish();
    if (renamed !== name) {
      onRename(renamed);
    }
  };

  return (
    <Input
      className={mergeClasses(styles.titleInput, "nodrag")}
      size="small"
      autoFocus
      value={draft}
      placeholder={typeTitle}
      maxLength={maxNameLength}
      aria-label="Module name"
      onChange={(_, data) => setDraft(data.value)}
      onBlur={save}
      onKeyDown={(event) => {
        // keep keys like Delete from reaching the graph, which would remove the module
        event.stopPropagation();
        if (event.key === "Enter") {
          save();
        } else if (event.key === "Escape") {
          finish();
        }
      }}
    />
  );
}

function Ports({
  type,
  port,
  index,
  count,
}: {
  type: "source" | "target";
  port: string;
  index: number;
  count: number;
}) {
  const styles = useStyles();
  const top = `${((index + 1) / (count + 1)) * 100}%`;

  return (
    <Handle
      id={port}
      type={type}
      position={type === "target" ? Position.Left : Position.Right}
      className={styles.port}
      style={{ top }}
    >
      {count > 1 && (
        <span className={styles.portLabel} style={type === "target" ? { left: 0 } : { right: 0 }}>
          {port}
        </span>
      )}
    </Handle>
  );
}
