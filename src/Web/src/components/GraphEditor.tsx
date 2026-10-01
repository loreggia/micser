import { Spinner, makeStyles } from "@fluentui/react-components";
import {
  getCreateConnectionMutationOptions,
  getDeleteConnectionMutationOptions,
  getDeleteModuleMutationOptions,
  useGetConnections,
  useGetModules,
  useGetModuleTypes,
  useModuleUpdate,
  type ModuleDto,
} from "@micser/web-sdk";
import { useMutation } from "@tanstack/react-query";
import {
  Background,
  Controls,
  ReactFlow,
  useEdgesState,
  useNodesState,
  type Connection,
  type Edge,
  type NodeTypes,
  type XYPosition,
} from "@xyflow/react";
import "@xyflow/react/dist/style.css";
import { useEffect, useMemo } from "react";
import { useNotifyError } from "../notifications";
import { widgets } from "../plugins";
import { ModuleNode, type ModuleNodeType } from "./ModuleNode";

const nodeTypes: NodeTypes = { module: ModuleNode };

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

/**
 * The routing graph: modules as nodes, connections as edges. Changes go to the engine; the graph follows the engine's
 * notifications, except for positions while a node is being dragged.
 */
export function GraphEditor() {
  const styles = useStyles();
  const { data: modules } = useGetModules();
  const { data: connections } = useGetConnections();
  const { data: moduleTypes } = useGetModuleTypes();
  const [nodes, setNodes, onNodesChange] = useNodesState<ModuleNodeType>([]);
  const [edges, setEdges, onEdgesChange] = useEdgesState<Edge>([]);
  const update = useModuleUpdate();
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
  }, [modules, typesByName, setNodes]);

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

  if (!modules || !connections || !moduleTypes) {
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

  return (
    <ReactFlow<ModuleNodeType, Edge>
      nodes={nodes}
      edges={edges}
      nodeTypes={nodeTypes}
      onNodesChange={onNodesChange}
      onEdgesChange={onEdgesChange}
      onNodeDragStop={(_, __, dragged) => dragged.forEach(moveModule)}
      onConnect={(connection: Connection) =>
        connect.mutate({
          data: {
            sourceModuleId: connection.source,
            sourcePort: connection.sourceHandle ?? "",
            targetModuleId: connection.target,
            targetPort: connection.targetHandle ?? "",
          },
        })
      }
      isValidConnection={(connection) => connection.source !== connection.target}
      onNodesDelete={(deleted) => deleted.forEach((node) => remove.mutate({ id: node.id }))}
      onEdgesDelete={(deleted) => deleted.forEach((edge) => disconnect.mutate({ id: edge.id }))}
      deleteKeyCode={["Delete", "Backspace"]}
      colorMode="system"
      fitView
      minZoom={0.25}
    >
      <Background />
      <Controls />
    </ReactFlow>
  );
}
