import { Menu, MenuItem, MenuList, MenuPopover, Spinner, makeStyles } from "@fluentui/react-components";
import {
  getCreateConnectionMutationOptions,
  getDeleteConnectionMutationOptions,
  getDeleteModuleMutationOptions,
  useGetConnections,
  useGetModules,
  useGetModuleTypes,
  useModuleUpdate,
  usePreferences,
  type CreateConnectionRequest,
  type ModuleDto,
} from "@micser/web-sdk";
import { useMutation } from "@tanstack/react-query";
import {
  Background,
  Controls,
  ReactFlow,
  useEdgesState,
  useNodesState,
  useReactFlow,
  type Connection,
  type Edge,
  type FinalConnectionState,
  type NodeTypes,
  type XYPosition,
} from "@xyflow/react";
import "@xyflow/react/dist/style.css";
import { useEffect, useMemo, useState } from "react";
import { useNotifyError } from "../notifications";
import { usePluginWidgets } from "../plugins";
import { ModuleNode, type ModuleNodeType } from "./ModuleNode";
import { useAddModule, useModuleTypeChoices } from "./useAddModule";

const nodeTypes: NodeTypes = { module: ModuleNode };

/** The grid modules snap to; the background dots use the same spacing. */
const gridSize = 20;

/** About the width of a module, to place a new module left of an input it connects to. */
const moduleWidth = 260;

const useStyles = makeStyles({
  loading: {
    display: "grid",
    placeItems: "center",
  },
});

/** Position of a module without a stored position: a grid by index. */
function defaultPosition(index: number): XYPosition {
  return { x: 40 + (index % 4) * 320, y: 40 + Math.floor(index / 4) * 280 };
}

/** A connection dropped on empty space: the menu for a new module opens there. */
interface PendingConnection {
  /** Screen position of the drop. */
  point: XYPosition;
  moduleId: string;
  port: string;
  /** Whether the drag started at an output, so the new module connects with its input. */
  fromOutput: boolean;
}

/**
 * The routing graph: modules as nodes, connections as edges. Changes go to the engine; the graph follows the engine's
 * notifications, except for positions while a node is being dragged. Connections can be dragged to other ports, and a
 * connection dropped on empty space offers to add a module there, connected to it.
 */
export function GraphEditor() {
  const styles = useStyles();
  const { data: modules } = useGetModules();
  const { data: connections } = useGetConnections();
  const { data: moduleTypes } = useGetModuleTypes();
  const moduleTypeChoices = useModuleTypeChoices();
  const { widgets, isLoading: isLoadingWidgets } = usePluginWidgets();
  const [preferences] = usePreferences();
  const [nodes, setNodes, onNodesChange] = useNodesState<ModuleNodeType>([]);
  const [edges, setEdges, onEdgesChange] = useEdgesState<Edge>([]);
  const [pending, setPending] = useState<PendingConnection>();
  const { screenToFlowPosition } = useReactFlow();
  const update = useModuleUpdate();
  const addModule = useAddModule();
  const notifyError = useNotifyError();

  const connect = useMutation({
    ...getCreateConnectionMutationOptions(),
    onError: (error) => notifyError("Connecting failed", error),
  });
  const disconnect = useMutation({
    ...getDeleteConnectionMutationOptions(),
    onError: (error) => notifyError("Disconnecting failed", error),
  });
  const remove = useMutation({
    ...getDeleteModuleMutationOptions(),
    onError: (error) => notifyError("Removing the module failed", error),
  });

  const typesByName = useMemo(() => new Map(moduleTypes?.map((type) => [type.type, type])), [moduleTypes]);

  useEffect(() => {
    if (!modules) {
      return;
    }

    setNodes((current) =>
      modules.map((module, index) => {
        const existing = current.find((node) => node.id === module.id);
        const data = { module, moduleType: typesByName.get(module.type), widget: widgets.get(module.type) };
        const position = module.position ?? existing?.position ?? defaultPosition(index);

        return existing
          ? { ...existing, data, position: existing.dragging ? existing.position : position }
          : { id: module.id, type: "module" as const, position, data };
      })
    );
  }, [modules, typesByName, widgets, setNodes]);

  useEffect(() => {
    if (!connections) {
      return;
    }

    setEdges((current) =>
      connections.map((connection) => ({
        id: connection.id,
        source: connection.sourceModuleId,
        sourceHandle: connection.sourcePort,
        target: connection.targetModuleId,
        targetHandle: connection.targetPort,
        selected: current.find((edge) => edge.id === connection.id)?.selected,
      }))
    );
  }, [connections, setEdges]);

  if (!modules || !connections || !moduleTypes || isLoadingWidgets) {
    return (
      <div className={styles.loading}>
        <Spinner label="Connecting to the engine" />
      </div>
    );
  }

  const moveModule = (node: ModuleNodeType) => {
    const module = modules.find((m) => m.id === node.id);
    if (module) {
      update({ ...module, position: { x: Math.round(node.position.x), y: Math.round(node.position.y) } } as ModuleDto);
    }
  };

  const toRequest = (connection: Connection): CreateConnectionRequest => ({
    sourceModuleId: connection.source,
    sourcePort: connection.sourceHandle ?? "",
    targetModuleId: connection.target,
    targetPort: connection.targetHandle ?? "",
  });

  // the new connection first, so the old one stays if the engine rejects it (e.g. a cycle)
  const reconnect = async (oldEdge: Edge, connection: Connection) => {
    const unchanged =
      oldEdge.source === connection.source &&
      oldEdge.sourceHandle === connection.sourceHandle &&
      oldEdge.target === connection.target &&
      oldEdge.targetHandle === connection.targetHandle;
    if (unchanged) {
      return;
    }

    try {
      await connect.mutateAsync({ data: toRequest(connection) });
    } catch {
      return;
    }

    disconnect.mutate({ id: oldEdge.id });
  };

  const onConnectEnd = (event: MouseEvent | TouchEvent, state: FinalConnectionState) => {
    if (state.isValid || state.toNode || !state.fromNode || !state.fromHandle?.id) {
      return;
    }

    const point = "changedTouches" in event ? event.changedTouches[0] : event;
    const connection = {
      point: { x: point.clientX, y: point.clientY },
      moduleId: state.fromNode.id,
      port: state.fromHandle.id,
      fromOutput: state.fromHandle.type === "source",
    };

    // after the click that follows the drop, which would close the menu again as a click outside of it
    window.setTimeout(() => setPending(connection));
  };

  const addConnected = async (type: string) => {
    if (!pending) {
      return;
    }

    setPending(undefined);
    const moduleType = typesByName.get(type);
    const drop = screenToFlowPosition(pending.point);
    const position = pending.fromOutput ? { x: drop.x, y: drop.y - 40 } : { x: drop.x - moduleWidth, y: drop.y - 40 };
    const module = await addModule(type, position);
    const port = pending.fromOutput ? moduleType?.inputs[0] : moduleType?.outputs[0];
    if (!module || !port) {
      return;
    }

    connect.mutate({
      data: pending.fromOutput
        ? { sourceModuleId: pending.moduleId, sourcePort: pending.port, targetModuleId: module.id, targetPort: port }
        : { sourceModuleId: module.id, sourcePort: port, targetModuleId: pending.moduleId, targetPort: pending.port },
    });
  };

  return (
    <>
      <ReactFlow<ModuleNodeType, Edge>
        nodes={nodes}
        edges={edges}
        nodeTypes={nodeTypes}
        onNodesChange={onNodesChange}
        onEdgesChange={onEdgesChange}
        onNodeDragStop={(_, __, dragged) => dragged.forEach(moveModule)}
        onConnect={(connection) => connect.mutate({ data: toRequest(connection) })}
        onConnectEnd={onConnectEnd}
        onReconnect={(oldEdge, connection) => void reconnect(oldEdge, connection)}
        isValidConnection={(connection) => connection.source !== connection.target}
        onNodesDelete={(deleted) => deleted.forEach((node) => remove.mutate({ id: node.id }))}
        onEdgesDelete={(deleted) => deleted.forEach((edge) => disconnect.mutate({ id: edge.id }))}
        deleteKeyCode={["Delete", "Backspace"]}
        snapToGrid={preferences.snapToGrid}
        snapGrid={[gridSize, gridSize]}
        colorMode="system"
        fitView
        minZoom={0.25}
      >
        <Background gap={gridSize} />
        <Controls />
      </ReactFlow>
      <Menu
        open={pending !== undefined}
        onOpenChange={(_, data) => !data.open && setPending(undefined)}
        positioning={{ target: pending && pointTarget(pending.point), position: "below", align: "start" }}
      >
        <MenuPopover>
          <MenuList>
            {moduleTypeChoices
              .filter((type) => (pending?.fromOutput ? type.inputs.length > 0 : type.outputs.length > 0))
              .map((type) => (
                <MenuItem key={type.type} onClick={() => void addConnected(type.type)}>
                  {type.title}
                </MenuItem>
              ))}
          </MenuList>
        </MenuPopover>
      </Menu>
    </>
  );
}

/** A zero-size positioning target at a screen point. */
function pointTarget(point: XYPosition) {
  return {
    getBoundingClientRect: () => ({
      x: point.x,
      y: point.y,
      left: point.x,
      top: point.y,
      right: point.x,
      bottom: point.y,
      width: 0,
      height: 0,
    }),
  };
}
