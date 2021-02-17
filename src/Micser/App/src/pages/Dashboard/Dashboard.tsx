import React, { MouseEvent, useCallback, useContext, useEffect, useMemo, useState } from "react";
import styled from "styled-components";
import ReactFlow, {
    ArrowHeadType,
    Background,
    BackgroundVariant,
    Connection,
    Controls,
    Edge,
    FlowElement,
    FlowTransform,
    Node,
    OnLoadParams,
} from "react-flow-renderer";
import { Card, Drawer, Dropdown, Menu, Modal, Space, Typography } from "antd";
import { AppstoreAddOutlined, ExclamationCircleOutlined } from "@ant-design/icons";
import { Module, ModuleConnection, ModuleDescription, useApi, useGetApi, WidgetType } from "micser-common";

import { PageContainer } from "~/components";
import { Widget, WidgetTypesContext } from "./Widget";

import { PluginsContext } from "~/hooks";
import { getRelativeCoordinates } from "~/utils";
import { Loader } from "~/components";
import { CustomEdge } from "./CustomEdge";

const FixedButton = styled.div`
    position: fixed;
    right: 32px;
    top: 32px;
    padding: 16px;
    z-index: 2147483640;
    cursor: pointer;
    transition: all 0.2s;
    border-radius: 100%;
    background: black;
    box-shadow: 0 0 16px white, inset 0 0 2px white;

    &:hover {
        background: rgba(32, 32, 32, 1);
        box-shadow: 0 0 16px white, inset 0 0 8px white;
    }
`;

const WidgetListCard = styled(Card)``;

const { Text, Title } = Typography;

const ModuleDescriptionKey = "moduledescription";
const DragOffsetKey = "dragoffset";

const GridSize = 10;

const ContextMenuActions = {
    DeleteModule: "DeleteModule",
    DeleteConnections: "DeleteConnections",
    DeleteConnectionsIn: "DeleteConnectionsIn",
    DeleteConnectionsOut: "DeleteConnectionsOut",
    DeleteAll: "DeleteAll",
};

export const Dashboard = () => {
    const plugins = useContext(PluginsContext);
    const [elements, setElements] = useState<FlowElement[]>([]);
    const [isAddDrawerOpen, setIsAddDrawerOpen] = useState(false);
    const [flowTransform, setFlowTransform] = useState<FlowTransform>({ x: 0, y: 0, zoom: 1 });
    const [contextMenuTarget, setContextMenuTarget] = useState<Node<Module>>();

    const [modules, { isLoading: isLoadingModules, refresh: loadModules }] = useGetApi<Module[]>("Modules");
    const [modulesApi, { isLoading: isLoadingModulesApi }] = useApi<Module>("Modules");
    const [moduleConnections, { isLoading: isLoadingModuleConnections, refresh: loadModuleConnections }] = useGetApi<
        ModuleConnection[]
    >("/ModuleConnections");
    const [moduleConnectionsApi, { isLoading: isLoadingModuleConnectionsApi }] = useApi<ModuleConnection>(
        "/ModuleConnections"
    );
    const [moduleDescriptions, { isLoading: isLoadingModuleDescriptions }] = useGetApi<ModuleDescription[]>(
        "/Modules/Descriptions"
    );

    const widgetTypes = useMemo(() => {
        return plugins.reduce<WidgetType[]>((prev, curr) => (curr.widgets ? prev.concat(curr.widgets) : prev), []);
    }, [plugins]);

    useEffect(() => {
        if (modules && moduleConnections) {
            const moduleElements = modules.map<FlowElement>((dto) => ({
                id: String(dto.id),
                type: "Widget",
                position: { x: Number(dto.state.left), y: Number(dto.state.top) },
                data: dto,
            }));
            const connectionElements = moduleConnections.map<FlowElement>((dto) => {
                const source = modules.find((m) => m.id === dto.sourceId);
                const target = modules.find((m) => m.id === dto.targetId);
                return {
                    id: `c${dto.id}`,
                    source: String(dto.sourceId),
                    target: String(dto.targetId),
                    sourceHandle: dto.sourceConnectorName,
                    targetHandle: dto.targetConnectorName,
                    // animated: true,
                    label: `${source?.state.title || source?.type} -> ${target?.state.title || target?.type}`,
                    arrowHeadType: ArrowHeadType.ArrowClosed,
                    type: "Custom",
                };
            });

            setElements(moduleElements.concat(connectionElements));
        }
    }, [modules, moduleConnections]);

    const nodeTypes = useMemo(() => ({ Widget: Widget }), []);
    const edgeTypes = useMemo(() => ({ Custom: CustomEdge }), []);

    const deleteAllModules = useCallback(async () => {
        await modulesApi?.delete("");
        loadModules();
    }, [modulesApi]);

    const handleDragStart = (e: React.DragEvent, moduleDescription: ModuleDescription) => {
        e.dataTransfer.setData(ModuleDescriptionKey, JSON.stringify(moduleDescription));
        const offset = getRelativeCoordinates(e);
        e.dataTransfer.setData(DragOffsetKey, JSON.stringify(offset));
    };

    const handleDragOver = (e: React.DragEvent) => {
        if (e.dataTransfer.types.includes(ModuleDescriptionKey)) {
            e.preventDefault();
        }
    };

    const handleDrop = async (e: React.DragEvent) => {
        const data = e.dataTransfer.getData(ModuleDescriptionKey);
        if (data) {
            const moduleDescription = JSON.parse(data);

            const position = getRelativeCoordinates(e);

            const offsetData = e.dataTransfer.getData(DragOffsetKey);
            if (offsetData) {
                const offset = JSON.parse(offsetData);
                position.x -= offset.x;
                position.y -= offset.y;
            }

            // snap to grid
            position.x = position.x - (position.x % GridSize);
            position.y = position.y - (position.y % GridSize);

            const module = {
                type: moduleDescription.name,
                state: {
                    left: position.x - flowTransform.x,
                    top: position.y - flowTransform.y,
                },
            };

            await modulesApi?.post("", module);
            loadModules();
        }
    };

    const handleNodeDragStop = async (_: MouseEvent, node: Node<Module>) => {
        const { data: module } = node;

        if (module) {
            const { x, y } = node.position;
            module.state.left = x.toString();
            module.state.top = y.toString();

            await modulesApi?.put(module.id, module);
            loadModules();
        }
    };

    const handleConnect = async ({ source, sourceHandle, target, targetHandle }: Edge | Connection) => {
        console.log(target);

        const connection = {
            sourceId: Number(source),
            targetId: Number(target),
            sourceConnectorName: sourceHandle,
            targetConnectorName: targetHandle,
        };

        await moduleConnectionsApi?.post("", connection);
        loadModuleConnections();
    };

    const handleEdgeUpdate = async (oldEdge: Edge, newConnection: Connection) => {
        let id = oldEdge.id;
        id = id.substring(1, id.length);
        const { source, sourceHandle, target, targetHandle } = newConnection;

        const connection = {
            id: Number(id),
            sourceId: Number(source),
            targetId: Number(target),
            sourceConnectorName: sourceHandle,
            targetConnectorName: targetHandle,
        };

        await moduleConnectionsApi?.put(connection.id, connection);
        loadModuleConnections();
    };

    const handleMoveEnd = (transform?: FlowTransform) => {
        if (transform) {
            setFlowTransform(transform);
        }
    };

    const handlePaneContextMenu = (_: React.MouseEvent) => {
        setContextMenuTarget(undefined);
    };

    const handleNodeContextMenu = (_: React.MouseEvent, node: Node) => {
        setContextMenuTarget(node);
    };

    const handleLoad = (reactFlowInstance: OnLoadParams) => reactFlowInstance.fitView();

    const isLoading =
        isLoadingModules ||
        isLoadingModulesApi ||
        isLoadingModuleConnections ||
        isLoadingModuleConnectionsApi ||
        isLoadingModuleDescriptions;

    const contextMenu = useMemo(() => {
        const handleContextMenuClick = async ({ key }: { key: React.Key }) => {
            switch (key) {
                case ContextMenuActions.DeleteAll:
                    Modal.confirm({
                        title: "Delete All Modules",
                        icon: <ExclamationCircleOutlined />,
                        content: "Are you sure you want to delete all modules?",
                        okText: "Yes",
                        cancelText: "Cancel",
                        onOk: () => deleteAllModules(),
                    });
                    break;
                case ContextMenuActions.DeleteModule:
                    if (contextMenuTarget?.data) {
                        await modulesApi?.delete(contextMenuTarget.data.id);
                        loadModules();
                    }
                    break;
                case ContextMenuActions.DeleteConnections:
                    break;
                case ContextMenuActions.DeleteConnectionsIn:
                    break;
                case ContextMenuActions.DeleteConnectionsOut:
                    break;
            }
        };

        return (
            <Menu onClick={handleContextMenuClick}>
                {contextMenuTarget ? (
                    <>
                        <Menu.Item key="DeleteModule">Delete Module</Menu.Item>
                        <Menu.Item key="DeleteConnections">Delete Connections</Menu.Item>
                        <Menu.Item key="DeleteConnectionsIn">Delete Incoming Connections</Menu.Item>
                        <Menu.Item key="DeleteConnectionsOut">Delete Outgoing Connections</Menu.Item>
                    </>
                ) : (
                    <>
                        <Menu.Item key="DeleteAll">Delete All</Menu.Item>
                    </>
                )}
            </Menu>
        );
    }, [contextMenuTarget, deleteAllModules]);

    const handleConnectStart = (...e: any) => {
        console.log("handleConnectStart");
        console.log(e);
    };
    const handleConnectStop = (...e: any) => {
        console.log("handleConnectStop");
        console.log(e);
    };
    const handleConnectEnd = (...e: any) => {
        console.log("handleConnectEnd");
        console.log(e);
    };
    const handleElementsRemove = (...e: any) => {
        console.log("handleElementsRemove");
        console.log(e);
    };

    return (
        <PageContainer noPadding>
            <Loader isVisible={isLoading} />
            <WidgetTypesContext.Provider value={widgetTypes}>
                <Dropdown overlay={contextMenu} trigger={["contextMenu"]}>
                    <ReactFlow
                        elements={elements}
                        nodeTypes={nodeTypes}
                        edgeTypes={edgeTypes}
                        snapToGrid
                        snapGrid={[GridSize, GridSize]}
                        defaultZoom={flowTransform.zoom}
                        defaultPosition={[flowTransform.x, flowTransform.y]}
                        style={{ width: "100%", height: "100%" }}
                        onLoad={handleLoad}
                        onDragOver={handleDragOver}
                        onDrop={handleDrop}
                        onNodeDragStop={handleNodeDragStop}
                        onEdgeUpdate={handleEdgeUpdate}
                        onConnect={handleConnect}
                        onConnectStart={handleConnectStart}
                        onConnectStop={handleConnectStop}
                        onConnectEnd={handleConnectEnd}
                        onElementsRemove={handleElementsRemove}
                        onlyRenderVisibleElements={false}
                        onMoveEnd={handleMoveEnd}
                        onPaneContextMenu={handlePaneContextMenu}
                        onNodeContextMenu={handleNodeContextMenu}
                    >
                        <Controls />
                        <Background variant={BackgroundVariant.Dots} size={1} gap={10} color="#333" />
                    </ReactFlow>
                </Dropdown>
                <Drawer visible={isAddDrawerOpen} onClose={() => setIsAddDrawerOpen(false)} width="300px" mask={false}>
                    <Title level={2}>Add Widget</Title>
                    <Text type="secondary">Drag a widget from the list to the dashboard.</Text>
                    <Space direction="vertical" size="middle" style={{ width: "100%" }}>
                        {moduleDescriptions &&
                            moduleDescriptions.map((description) => (
                                <WidgetListCard
                                    key={description.name}
                                    size="small"
                                    title={description.title}
                                    hoverable
                                    draggable
                                    onDragStart={(e) => handleDragStart(e, description)}
                                >
                                    {description.description}
                                </WidgetListCard>
                            ))}
                    </Space>
                </Drawer>
                <FixedButton className="primary" onClick={() => setIsAddDrawerOpen(true)} hidden={isAddDrawerOpen}>
                    <AppstoreAddOutlined style={{ fontSize: "32px" }} />
                </FixedButton>
            </WidgetTypesContext.Provider>
        </PageContainer>
    );
};
