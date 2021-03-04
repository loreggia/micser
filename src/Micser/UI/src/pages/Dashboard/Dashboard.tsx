import React, { MouseEvent, useCallback, useContext, useEffect, useMemo, useState } from "react";
import styled from "styled-components";
import { useTranslation } from "react-i18next";
import ReactFlow, {
    ArrowHeadType,
    Background,
    BackgroundVariant,
    Connection,
    ConnectionMode,
    Controls,
    Edge,
    FlowElement,
    FlowTransform,
    isEdge,
    isNode,
    Node,
} from "react-flow-renderer";
import { Card, Drawer, Dropdown, Menu, Modal, Space, Typography } from "antd";
import { AppstoreAddOutlined, ExclamationCircleOutlined } from "@ant-design/icons";
import {
    Contexts,
    DashboardContext,
    Loader,
    Module,
    ModuleConnection,
    ModuleDefinition,
    ModuleState,
    useApi,
    useGetApi,
    Widget as WidgetType,
} from "micser-common";

import { PageContainer } from "~/components";
import { Widget } from "./Widget";

import { PluginsContext } from "~/hooks";
import { getRelativeCoordinates } from "~/utils";
import { CustomEdge } from "./CustomEdge";
import { useGetWidgetType } from "./utils";

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
    DeleteConnection: "DeleteConnection",
    DeleteConnections: "DeleteConnections",
    DeleteConnectionsIn: "DeleteConnectionsIn",
    DeleteConnectionsOut: "DeleteConnectionsOut",
    DeleteAll: "DeleteAll",
};

type ContextMenuTarget = Node<Module> | Edge<ModuleConnection>;

export const Dashboard = () => {
    const { t } = useTranslation();

    const plugins = useContext(PluginsContext);
    const [elements, setElements] = useState<FlowElement[]>([]);
    const [isAddDrawerOpen, setIsAddDrawerOpen] = useState(false);
    const [flowTransform, setFlowTransform] = useState<FlowTransform>({ x: 0, y: 0, zoom: 1 });
    const [contextMenuTarget, setContextMenuTarget] = useState<ContextMenuTarget>();

    const [modules, { isLoading: isLoadingModules, refresh: loadModules }] = useGetApi<Module[]>("Modules");
    const [modulesApi, { isLoading: isLoadingModulesApi }] = useApi<Module>("Modules");
    const [moduleConnections, { isLoading: isLoadingModuleConnections, refresh: loadModuleConnections }] = useGetApi<
        ModuleConnection[]
    >("/ModuleConnections");
    const [moduleConnectionsApi, { isLoading: isLoadingModuleConnectionsApi }] = useApi<ModuleConnection>(
        "/ModuleConnections"
    );

    const widgetTypes = useMemo(() => {
        return plugins.reduce<WidgetType[]>((prev, curr) => (curr.widgets ? prev.concat(curr.widgets) : prev), []);
    }, [plugins]);

    const getWidgetType = useGetWidgetType(widgetTypes);

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
                const { title: sourceTitle } = getWidgetType(source);
                const target = modules.find((m) => m.id === dto.targetId);
                const { title: targetTitle } = getWidgetType(target);
                return {
                    id: `c${dto.id}`,
                    source: String(dto.sourceId),
                    target: String(dto.targetId),
                    sourceHandle: dto.sourceConnectorName,
                    targetHandle: dto.targetConnectorName,
                    animated: true,
                    label: `${sourceTitle} \u2192 ${targetTitle}`,
                    arrowHeadType: ArrowHeadType.ArrowClosed,
                    type: "Custom",
                };
            });

            setElements(moduleElements.concat(connectionElements));
        }
    }, [modules, moduleConnections, getWidgetType]);

    const nodeTypes = useMemo(() => ({ Widget: Widget }), []);
    const edgeTypes = useMemo(() => ({ Custom: CustomEdge }), []);

    const deleteAllModules = useCallback(async () => {
        await modulesApi?.delete("");
        loadModules();
    }, [modulesApi, loadModules]);

    const handleDragStart = (e: React.DragEvent, moduleDescription: ModuleDefinition) => {
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

    const saveNodePosition = async (node: Node<Module>) => {
        const { data: module } = node;

        if (module) {
            const { x, y } = node.position;
            module.state.left = x.toString();
            module.state.top = y.toString();

            await modulesApi?.put(module.id, module);
        }
    };

    const handleNodeDragStop = async (_: MouseEvent, node: Node<Module>) => {
        await saveNodePosition(node);
        loadModules();
    };

    const handleSelectionDragStop = async (_: React.MouseEvent, nodes: Node<Module>[]) => {
        const promises = nodes.map((node) => saveNodePosition(node));
        await Promise.all(promises);
        loadModules();
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

    const handleMove = (transform?: FlowTransform) => {
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

    const handleEdgeContextMenu = (_: React.MouseEvent, edge: Edge) => {
        setContextMenuTarget(edge);
    };

    // const handleLoad = (reactFlowInstance: OnLoadParams) => reactFlowInstance.fitView();

    const isLoading =
        isLoadingModules || isLoadingModulesApi || isLoadingModuleConnections || isLoadingModuleConnectionsApi;

    const contextMenu = useMemo(() => {
        const handleContextMenuClick = async ({ key }: { key: React.Key }) => {
            switch (key) {
                case ContextMenuActions.DeleteAll:
                    Modal.confirm({
                        title: t("dashboard.contextMenuActions.deleteAll"),
                        icon: <ExclamationCircleOutlined />,
                        content: t("dashboard.contextMenuActions.deleteAllConfirmation"),
                        okText: t("global.yes"),
                        cancelText: t("global.cancel"),
                        onOk: () => deleteAllModules(),
                    });
                    break;
                case ContextMenuActions.DeleteModule:
                    if (contextMenuTarget?.data) {
                        await modulesApi?.delete(contextMenuTarget.data.id);
                        loadModules();
                    }
                    break;
                case ContextMenuActions.DeleteConnection:
                    if (contextMenuTarget?.data) {
                        await moduleConnectionsApi?.delete(contextMenuTarget.data.id);
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

        let menuItems: React.ReactNode;
        if (!contextMenuTarget) {
            menuItems = (
                <>
                    <Menu.Item key={ContextMenuActions.DeleteAll}>
                        {t("dashboard.contextMenuActions.deleteAll")}
                    </Menu.Item>
                </>
            );
        } else if (isNode(contextMenuTarget)) {
            menuItems = (
                <>
                    <Menu.Item key={ContextMenuActions.DeleteModule}>
                        {t("dashboard.contextMenuActions.deleteModule")}
                    </Menu.Item>
                    <Menu.Item key={ContextMenuActions.DeleteConnections}>
                        {t("dashboard.contextMenuActions.deleteConnections")}
                    </Menu.Item>
                    <Menu.Item key={ContextMenuActions.DeleteConnectionsIn}>
                        {t("dashboard.contextMenuActions.deleteIncomingConnections")}
                    </Menu.Item>
                    <Menu.Item key={ContextMenuActions.DeleteConnectionsOut}>
                        {t("dashboard.contextMenuActions.deleteOutgoingConnections")}
                    </Menu.Item>
                </>
            );
        } else if (isEdge(contextMenuTarget)) {
            menuItems = (
                <>
                    <Menu.Item key={ContextMenuActions.DeleteConnection}>
                        {t("dashboard.contextMenuActions.deleteConnection")}
                    </Menu.Item>
                </>
            );
        } else {
            menuItems = <>Error</>;
        }

        return <Menu onClick={handleContextMenuClick}>{menuItems}</Menu>;
    }, [contextMenuTarget, deleteAllModules, loadModules, modulesApi, t]);

    const handleElementsRemove = (...e: any) => {
        console.log("handleElementsRemove");
        console.log(e);
    };

    const handleStateChanged = useCallback(
        async (module: Module, state: Partial<ModuleState>) => {
            await modulesApi?.put(module.id, { ...module, state: { ...module.state, ...state } });
            loadModules();
        },
        [modulesApi, loadModules]
    );

    const dashboardContext = useMemo<DashboardContext>(
        () => ({
            onStateChanged: handleStateChanged,
        }),
        [handleStateChanged]
    );

    return (
        <PageContainer noPadding>
            <Loader isVisible={isLoading} />
            <Contexts.widgetTypes.Provider value={widgetTypes}>
                <Contexts.dashboard.Provider value={dashboardContext}>
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
                            selectNodesOnDrag
                            connectionMode={ConnectionMode.Strict}
                            // onLoad={handleLoad}
                            onDragOver={handleDragOver}
                            onDrop={handleDrop}
                            onNodeDragStop={handleNodeDragStop}
                            onEdgeUpdate={handleEdgeUpdate}
                            onConnect={handleConnect}
                            onElementsRemove={handleElementsRemove}
                            onlyRenderVisibleElements={false}
                            onMove={handleMove}
                            onMoveEnd={handleMove}
                            onPaneContextMenu={handlePaneContextMenu}
                            onNodeContextMenu={handleNodeContextMenu}
                            onEdgeContextMenu={handleEdgeContextMenu}
                            onSelectionDragStop={handleSelectionDragStop}
                        >
                            <Controls />
                            <Background variant={BackgroundVariant.Dots} size={1} gap={10} color="#333" />
                        </ReactFlow>
                    </Dropdown>
                </Contexts.dashboard.Provider>
                <Drawer visible={isAddDrawerOpen} onClose={() => setIsAddDrawerOpen(false)} width="300px" mask={false}>
                    <Title level={2}>{t("dashboard.widgetsDrawer.title")}</Title>
                    <Text type="secondary">{t("dashboard.widgetsDrawer.info")}</Text>
                    <Space direction="vertical" size="middle" style={{ width: "100%" }}>
                        {widgetTypes.map((widgetType) => {
                            return (
                                widgetType && (
                                    <WidgetListCard
                                        key={widgetType.name}
                                        size="small"
                                        title={t(widgetType.titleResource)}
                                        hoverable
                                        draggable
                                        onDragStart={(e) => handleDragStart(e, widgetType)}
                                    >
                                        {t(widgetType.descriptionResource)}
                                    </WidgetListCard>
                                )
                            );
                        })}
                    </Space>
                </Drawer>
                <FixedButton className="primary" onClick={() => setIsAddDrawerOpen(true)} hidden={isAddDrawerOpen}>
                    <AppstoreAddOutlined style={{ fontSize: "32px" }} />
                </FixedButton>
            </Contexts.widgetTypes.Provider>
        </PageContainer>
    );
};
