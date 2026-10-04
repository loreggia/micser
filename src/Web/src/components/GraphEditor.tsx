import {
  Menu,
  MenuDivider,
  MenuItem,
  MenuList,
  MenuPopover,
  Spinner,
  makeStyles,
  tokens,
} from "@fluentui/react-components";
import {
  getCreateConnectionMutationOptions,
  getCreateSubgraphMutationOptions,
  getDeleteConnectionMutationOptions,
  getDeleteModuleMutationOptions,
  getDeleteSubgraphMutationOptions,
  useGetConnections,
  useGetModules,
  useGetModuleTypes,
  useGetSubgraphs,
  useModuleUpdate,
  usePreferences,
  useSubgraphUpdate,
  type CreateConnectionRequest,
  type ModuleDto,
  type SubgraphDto,
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
import { useCallback, useEffect, useMemo, useState } from "react";
import { useNotifyError } from "../notifications";
import { usePluginWidgets } from "../plugins";
import { ModuleNode, type ModuleNodeType } from "./ModuleNode";
import { SubgraphNode, type SubgraphNodeType } from "./SubgraphNode";
import { frameAround, gridSize, proxyHandleId, resolveHandle, subgraphPadding, type ProxyPort } from "./subgraphs";
import { TemplatesSubmenu } from "./TemplatesSubmenu";
import { useAddModule, useInstantiateTemplate, useModuleTypeChoices } from "./useAddModule";

type GraphNode = ModuleNodeType | SubgraphNodeType;

const nodeTypes: NodeTypes = { module: ModuleNode, subgraph: SubgraphNode };

/** About the width of a module, to place a new module left of an input it connects to. */
const moduleWidth = 260;

const useStyles = makeStyles({
  loading: {
    display: "grid",
    placeItems: "center",
  },
  flow: {
    "& .react-flow__edge.selected .react-flow__edgeupdater": {
      fill: tokens.colorNeutralBackground1,
      stroke: tokens.colorBrandStroke1,
      strokeWidth: tokens.strokeWidthThick,
    },
  },
});

/** Position of a module without a stored position: a grid by index. */
function defaultPosition(index: number): XYPosition {
  return { x: 40 + (index % 4) * 320, y: 40 + Math.floor(index / 4) * 280 };
}

function round(position: XYPosition): XYPosition {
  return { x: Math.round(position.x), y: Math.round(position.y) };
}

/** Whether the point is inside the frame of an expanded subgraph. */
function contains(subgraph: SubgraphDto, point: XYPosition) {
  const { position, size } = subgraph;
  return (
    point.x >= position.x &&
    point.y >= position.y &&
    point.x <= position.x + size.width &&
    point.y <= position.y + size.height
  );
}

/** Whether the rectangle overlaps the frame of a subgraph. */
function overlaps(subgraph: SubgraphDto, rect: XYPosition & { width: number; height: number }) {
  const { position, size } = subgraph;
  return (
    rect.x < position.x + size.width &&
    rect.x + rect.width > position.x &&
    rect.y < position.y + size.height &&
    rect.y + rect.height > position.y
  );
}

/** Whether a context menu event comes from a text field, which keeps the browser's menu. */
function isEditable(event: { target: EventTarget | null }) {
  return (
    event.target instanceof HTMLElement && event.target.closest("input, textarea, [contenteditable='true']") !== null
  );
}

/**
 * The menu for adding a module, opened by a right click on empty space or a subgraph's frame, or by dropping a connection on empty
 * space.
 */
interface AddModuleMenu {
  /** Screen position of the click or drop, where the new module goes. */
  point: XYPosition;
  /** The dropped connection, which the new module completes. */
  connection?: {
    moduleId: string;
    port: string;
    /** Whether the drag started at an output, so the new module connects with its input. */
    fromOutput: boolean;
  };
  /** The subgraph whose frame was clicked, which the new module joins. */
  subgraph?: SubgraphDto;
}

/** The menu of modules, opened by a right click on a module or the selection. */
interface ModuleMenu {
  point: XYPosition;
  moduleIds: string[];
}

/**
 * The routing graph: modules as nodes, connections as edges, and subgraphs as frames around their modules (or, collapsed, as nodes
 * with the ports of the connections crossing their border). Changes go to the engine; the graph follows the engine's notifications,
 * except for positions while a node is being dragged and sizes while a subgraph is resized.
 *
 * A selected connection has handles at its ends that drag it to other ports; unselected ones have none, so a drag at a port with
 * several connections can't take the wrong one. A right click on empty space offers to add a module there, and so does a connection
 * dropped there, connecting the new module. Ctrl+G or the context menu groups the selected modules into a subgraph; dragging a module
 * onto or off a subgraph's frame moves it into or out of the subgraph.
 */
export function GraphEditor() {
  const styles = useStyles();
  const { data: modules } = useGetModules();
  const { data: connections } = useGetConnections();
  const { data: moduleTypes } = useGetModuleTypes();
  const { data: subgraphs } = useGetSubgraphs();
  const moduleTypeChoices = useModuleTypeChoices();
  const { widgets, isLoading: isLoadingWidgets } = usePluginWidgets();
  const [preferences] = usePreferences();
  const [nodes, setNodes, onNodesChange] = useNodesState<GraphNode>([]);
  const [edges, setEdges, onEdgesChange] = useEdgesState<Edge>([]);
  const [menu, setMenu] = useState<AddModuleMenu>();
  const [moduleMenu, setModuleMenu] = useState<ModuleMenu>();
  const { deleteElements, screenToFlowPosition, getInternalNode, getNodes, getNodesBounds } = useReactFlow<
    GraphNode,
    Edge
  >();
  const update = useModuleUpdate();
  const updateSubgraph = useSubgraphUpdate();
  const addModule = useAddModule();
  const instantiateTemplate = useInstantiateTemplate();
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
  const { mutate: createSubgraph } = useMutation({
    ...getCreateSubgraphMutationOptions(),
    onError: (error) => notifyError("Grouping failed", error),
  });
  const removeSubgraph = useMutation({
    ...getDeleteSubgraphMutationOptions(),
    onError: (error) => notifyError("Deleting the subgraph failed", error),
  });

  const typesByName = useMemo(() => new Map(moduleTypes?.map((type) => [type.type, type])), [moduleTypes]);

  // the collapsed subgraph of each module in one
  const collapsedSubgraphs = useMemo(() => {
    const collapsed = new Set(subgraphs?.filter((s) => s.isCollapsed).map((s) => s.id));
    return new Map(
      modules?.filter((m) => m.subgraphId && collapsed.has(m.subgraphId)).map((m) => [m.id, m.subgraphId!])
    );
  }, [modules, subgraphs]);

  // the ports of collapsed subgraphs: one per module port that a connection from or to the outside uses
  const proxyPorts = useMemo(() => {
    const ports = new Map<string, { inputs: ProxyPort[]; outputs: ProxyPort[] }>();
    const add = (subgraphId: string, direction: "in" | "out", moduleId: string, port: string) => {
      const entry = ports.get(subgraphId) ?? { inputs: [], outputs: [] };
      ports.set(subgraphId, entry);
      const list = direction === "in" ? entry.inputs : entry.outputs;
      const id = proxyHandleId(direction, moduleId, port);
      if (list.some((p) => p.id === id)) {
        return;
      }

      const module = modules?.find((m) => m.id === moduleId);
      const moduleType = module && typesByName.get(module.type);
      const title = module?.name || (module && widgets.get(module.type)?.title) || module?.type || port;
      const portCount = (direction === "in" ? moduleType?.inputs : moduleType?.outputs)?.length ?? 0;
      list.push({ id, label: portCount > 1 ? `${title} · ${port}` : title });
    };

    for (const connection of connections ?? []) {
      const source = collapsedSubgraphs.get(connection.sourceModuleId);
      const target = collapsedSubgraphs.get(connection.targetModuleId);
      if (source === target) {
        continue;
      }

      if (source) {
        add(source, "out", connection.sourceModuleId, connection.sourcePort);
      }

      if (target) {
        add(target, "in", connection.targetModuleId, connection.targetPort);
      }
    }

    return ports;
  }, [connections, modules, collapsedSubgraphs, typesByName, widgets]);

  useEffect(() => {
    if (!modules || !subgraphs) {
      return;
    }

    const subgraphsById = new Map(subgraphs.map((s) => [s.id, s]));
    setNodes((current) => {
      const existing = new Map(current.map((node) => [node.id, node]));

      // parents have to come before their children
      const subgraphNodes = subgraphs.map((subgraph): SubgraphNodeType => {
        const node = existing.get(subgraph.id) as SubgraphNodeType | undefined;
        const data = { subgraph, ...(proxyPorts.get(subgraph.id) ?? { inputs: [], outputs: [] }) };
        const size = subgraph.isCollapsed
          ? { width: undefined, height: undefined }
          : node?.resizing
            ? { width: node.width, height: node.height }
            : subgraph.size;
        const position = node?.dragging ? node.position : subgraph.position;

        return node
          ? { ...node, data, position, ...size }
          : { id: subgraph.id, type: "subgraph" as const, position, data, ...size };
      });

      const moduleNodes = modules.map((module, index): ModuleNodeType => {
        const node = existing.get(module.id) as ModuleNodeType | undefined;
        const subgraph = module.subgraphId ? subgraphsById.get(module.subgraphId) : undefined;
        const data = { module, moduleType: typesByName.get(module.type), widget: widgets.get(module.type), subgraph };
        const position = (node?.dragging ? node.position : module.position) ?? node?.position ?? defaultPosition(index);
        const placement = { parentId: subgraph?.id, hidden: subgraph?.isCollapsed ?? false };

        return node
          ? { ...node, data, position, ...placement }
          : { id: module.id, type: "module" as const, position, data, ...placement };
      });

      return [...subgraphNodes, ...moduleNodes];
    });
  }, [modules, subgraphs, typesByName, widgets, proxyPorts, setNodes]);

  useEffect(() => {
    if (!connections) {
      return;
    }

    // ends inside a collapsed subgraph attach to its proxy ports; connections within one stay between their hidden modules
    setEdges((current) =>
      connections.map((connection) => {
        const sourceSubgraph = collapsedSubgraphs.get(connection.sourceModuleId);
        const targetSubgraph = collapsedSubgraphs.get(connection.targetModuleId);
        const hidden = sourceSubgraph !== undefined && sourceSubgraph === targetSubgraph;
        const source = hidden ? undefined : sourceSubgraph;
        const target = hidden ? undefined : targetSubgraph;
        return {
          id: connection.id,
          source: source ?? connection.sourceModuleId,
          sourceHandle: source
            ? proxyHandleId("out", connection.sourceModuleId, connection.sourcePort)
            : connection.sourcePort,
          target: target ?? connection.targetModuleId,
          targetHandle: target
            ? proxyHandleId("in", connection.targetModuleId, connection.targetPort)
            : connection.targetPort,
          hidden,
          selected: current.find((edge) => edge.id === connection.id)?.selected,
        };
      })
    );
  }, [connections, collapsedSubgraphs, setEdges]);

  // expanded frames grow to contain their modules, e.g. after a module was added, moved in or expanded. Membership and positions
  // come from the engine's data: a dropped node still has its old parent until the module update reaches the cache.
  useEffect(() => {
    if (!modules || !subgraphs || nodes.some((node) => node.resizing)) {
      return;
    }

    const measured = new Map(nodes.map((node) => [node.id, node.measured]));
    for (const subgraph of subgraphs.filter((s) => !s.isCollapsed)) {
      let { width, height } = subgraph.size;
      for (const module of modules.filter((m) => m.subgraphId === subgraph.id && m.position)) {
        const size = measured.get(module.id);
        if (size?.width && size.height) {
          width = Math.max(width, module.position!.x + size.width + subgraphPadding);
          height = Math.max(height, module.position!.y + size.height + subgraphPadding);
        }
      }

      if (width > subgraph.size.width || height > subgraph.size.height) {
        updateSubgraph({
          ...subgraph,
          size: { width: Math.ceil(width / gridSize) * gridSize, height: Math.ceil(height / gridSize) * gridSize },
        });
      }
    }
  }, [nodes, modules, subgraphs, updateSubgraph]);

  const shownEdges = useMemo(
    () => edges.map((edge) => (edge.selected ? { ...edge, reconnectable: true } : edge)),
    [edges]
  );

  const groupModules = useCallback(
    (moduleIds: string[]) => {
      // modules already in a subgraph can't be grouped, also not together with others
      const members = getNodes().filter((node) => node.type === "module" && moduleIds.includes(node.id));
      if (members.length === 0 || members.some((node) => node.parentId)) {
        return;
      }

      createSubgraph({
        data: {
          name: null,
          ...frameAround(getNodesBounds(members)),
          moduleIds: members.map((node) => node.id),
          color: "Blue",
        },
      });
    },
    [getNodes, getNodesBounds, createSubgraph]
  );

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "g" && !isEditable(event)) {
        event.preventDefault();
        groupModules(
          getNodes()
            .filter((node) => node.selected)
            .map((node) => node.id)
        );
      }
    };

    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [groupModules, getNodes]);

  if (!modules || !connections || !moduleTypes || !subgraphs || isLoadingWidgets) {
    return (
      <div className={styles.loading}>
        <Spinner label="Connecting to the engine" />
      </div>
    );
  }

  // a module stays in its subgraph while it overlaps the frame; otherwise it joins the expanded subgraph under its center, if any
  const onNodeDragStop = (dragged: GraphNode[]) => {
    const draggedIds = new Set(dragged.map((node) => node.id));
    for (const node of dragged) {
      if (node.type === "subgraph") {
        const subgraph = subgraphs.find((s) => s.id === node.id);
        if (subgraph) {
          updateSubgraph({ ...subgraph, position: round(node.position) });
        }

        continue;
      }

      const module = modules.find((m) => m.id === node.id);
      if (!module) {
        continue;
      }

      // moved along with its subgraph
      if (node.parentId && draggedIds.has(node.parentId)) {
        continue;
      }

      const absolute = getInternalNode(node.id)?.internals.positionAbsolute ?? node.position;
      const width = node.measured?.width ?? 0;
      const height = node.measured?.height ?? 0;
      const center = { x: absolute.x + width / 2, y: absolute.y + height / 2 };
      const current = subgraphs.find((s) => s.id === module.subgraphId);
      const target =
        current && overlaps(current, { ...absolute, width, height })
          ? current
          : subgraphs.findLast((s) => !s.isCollapsed && !draggedIds.has(s.id) && contains(s, center));
      const position =
        (target?.id ?? null) === (module.subgraphId ?? null)
          ? node.position
          : { x: absolute.x - (target?.position.x ?? 0), y: absolute.y - (target?.position.y ?? 0) };

      update({ ...module, subgraphId: target?.id ?? null, position: round(position) } as ModuleDto);
    }
  };

  // below the frame, keeping their arrangement
  const removeFromSubgraph = (moduleIds: string[]) => {
    const members = modules.filter((m) => moduleIds.includes(m.id) && m.subgraphId);
    const top = Math.min(...members.map((m) => m.position?.y ?? 0));
    for (const module of members) {
      const subgraph = subgraphs.find((s) => s.id === module.subgraphId);
      const position = module.position ?? { x: 0, y: 0 };
      const offset = subgraph
        ? { x: subgraph.position.x, y: subgraph.position.y + subgraph.size.height + gridSize - top }
        : { x: 0, y: 0 };
      update({
        ...module,
        subgraphId: null,
        position: round({ x: position.x + offset.x, y: position.y + offset.y }),
      } as ModuleDto);
    }
  };

  const toRequest = (connection: Connection): CreateConnectionRequest => {
    const source = resolveHandle(connection.source, connection.sourceHandle);
    const target = resolveHandle(connection.target, connection.targetHandle);
    return {
      sourceModuleId: source.moduleId,
      sourcePort: source.port,
      targetModuleId: target.moduleId,
      targetPort: target.port,
    };
  };

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
    const { moduleId, port } = resolveHandle(state.fromNode.id, state.fromHandle.id);
    openMenu(setMenu, {
      point: { x: point.clientX, y: point.clientY },
      connection: { moduleId, port, fromOutput: state.fromHandle.type === "source" },
    });
  };

  const addFromMenu = async (type: string) => {
    if (!menu) {
      return;
    }

    setMenu(undefined);
    const point = screenToFlowPosition(menu.point);
    const { connection, subgraph } = menu;
    if (subgraph) {
      await addModule(type, round({ x: point.x - subgraph.position.x, y: point.y - subgraph.position.y }), subgraph.id);
      return;
    }

    if (!connection) {
      await addModule(type, point);
      return;
    }

    const moduleType = typesByName.get(type);
    const position = connection.fromOutput
      ? { x: point.x, y: point.y - 40 }
      : { x: point.x - moduleWidth, y: point.y - 40 };
    const module = await addModule(type, position);
    const port = connection.fromOutput ? moduleType?.inputs[0] : moduleType?.outputs[0];
    if (!module || !port) {
      return;
    }

    connect.mutate({
      data: connection.fromOutput
        ? {
            sourceModuleId: connection.moduleId,
            sourcePort: connection.port,
            targetModuleId: module.id,
            targetPort: port,
          }
        : {
            sourceModuleId: module.id,
            sourcePort: port,
            targetModuleId: connection.moduleId,
            targetPort: connection.port,
          },
    });
  };

  const openModuleMenu = (event: React.MouseEvent, moduleIds: string[]) => {
    event.preventDefault();
    if (moduleIds.length > 0) {
      openMenu(setModuleMenu, { point: { x: event.clientX, y: event.clientY }, moduleIds });
    }
  };

  const moduleMenuInSubgraph = moduleMenu?.moduleIds.some((id) => modules.find((m) => m.id === id)?.subgraphId);

  return (
    <>
      <ReactFlow<GraphNode, Edge>
        nodes={nodes}
        className={styles.flow}
        edges={shownEdges}
        nodeTypes={nodeTypes}
        onNodesChange={onNodesChange}
        onEdgesChange={onEdgesChange}
        onNodeDragStop={(_, __, dragged) => onNodeDragStop(dragged)}
        onConnect={(connection) => connect.mutate({ data: toRequest(connection) })}
        onConnectEnd={onConnectEnd}
        onPaneContextMenu={(event) => {
          event.preventDefault();
          openMenu(setMenu, { point: { x: event.clientX, y: event.clientY } });
        }}
        onNodeContextMenu={(event, node) => {
          if (isEditable(event)) {
            return;
          }

          if (node.type === "subgraph") {
            event.preventDefault();
            if (!node.data.subgraph.isCollapsed) {
              openMenu(setMenu, { point: { x: event.clientX, y: event.clientY }, subgraph: node.data.subgraph });
            }

            return;
          }

          const selected = getNodes().filter((n) => n.selected && n.type === "module");
          openModuleMenu(event, node.selected ? selected.map((n) => n.id) : [node.id]);
        }}
        onSelectionContextMenu={(event, selected) =>
          openModuleMenu(
            event,
            selected.filter((n) => n.type === "module").map((n) => n.id)
          )
        }
        onReconnect={(oldEdge, connection) => void reconnect(oldEdge, connection)}
        edgesReconnectable={false}
        elevateEdgesOnSelect
        elevateNodesOnSelect={false}
        isValidConnection={(connection) =>
          resolveHandle(connection.source, connection.sourceHandle).moduleId !==
          resolveHandle(connection.target, connection.targetHandle).moduleId
        }
        // the engine removes a deleted subgraph with its modules, and the connections of removed modules, so the editor
        // only deletes the other modules and the selected connections that remain
        onBeforeDelete={({ nodes: deleted, edges: deletedEdges }) => {
          const subgraphIds = new Set(deleted.filter((node) => node.type === "subgraph").map((node) => node.id));
          subgraphIds.forEach((id) => removeSubgraph.mutate({ id, params: { deleteModules: true } }));
          const removedByEngine = new Set([
            ...subgraphIds,
            ...nodes.filter((node) => node.parentId && subgraphIds.has(node.parentId)).map((node) => node.id),
          ]);
          return Promise.resolve({
            nodes: deleted.filter((node) => node.type === "module" && !removedByEngine.has(node.id)),
            edges: deletedEdges.filter(
              (edge) => edge.selected && !removedByEngine.has(edge.source) && !removedByEngine.has(edge.target)
            ),
          });
        }}
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
        open={menu !== undefined}
        onOpenChange={(_, data) => !data.open && setMenu(undefined)}
        positioning={{ target: menu && pointTarget(menu.point), position: "below", align: "start" }}
      >
        <MenuPopover>
          <MenuList>
            {moduleTypeChoices
              .filter(
                (type) =>
                  !menu?.connection || (menu.connection.fromOutput ? type.inputs.length > 0 : type.outputs.length > 0)
              )
              .map((type) => (
                <MenuItem key={type.type} onClick={() => void addFromMenu(type.type)}>
                  {type.title}
                </MenuItem>
              ))}
            {menu && !menu.connection && !menu.subgraph && (
              <>
                <MenuDivider />
                <TemplatesSubmenu
                  onInstantiate={(templateId) => {
                    setMenu(undefined);
                    instantiateTemplate(templateId, round(screenToFlowPosition(menu.point)));
                  }}
                />
              </>
            )}
          </MenuList>
        </MenuPopover>
      </Menu>
      <Menu
        open={moduleMenu !== undefined}
        onOpenChange={(_, data) => !data.open && setModuleMenu(undefined)}
        positioning={{ target: moduleMenu && pointTarget(moduleMenu.point), position: "below", align: "start" }}
      >
        <MenuPopover>
          <MenuList>
            {!moduleMenuInSubgraph && (
              <MenuItem secondaryContent="Ctrl+G" onClick={() => moduleMenu && groupModules(moduleMenu.moduleIds)}>
                Group
              </MenuItem>
            )}
            {moduleMenuInSubgraph && (
              <MenuItem onClick={() => moduleMenu && removeFromSubgraph(moduleMenu.moduleIds)}>
                Remove from subgraph
              </MenuItem>
            )}
            <MenuDivider />
            <MenuItem
              secondaryContent="Del"
              onClick={() => moduleMenu && void deleteElements({ nodes: moduleMenu.moduleIds.map((id) => ({ id })) })}
            >
              Delete
            </MenuItem>
          </MenuList>
        </MenuPopover>
      </Menu>
    </>
  );
}

// after the event that opens it, which the menu would otherwise take as a click outside of it and close again
function openMenu<T>(setMenu: (menu: T) => void, menu: T) {
  window.setTimeout(() => setMenu(menu));
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
