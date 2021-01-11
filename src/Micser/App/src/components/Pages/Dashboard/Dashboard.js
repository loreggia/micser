import React, { useCallback, useContext, useEffect, useMemo, useState } from "react";
import styled from "styled-components";
import ReactFlow, { Background, Controls } from "react-flow-renderer";
import { Card, Drawer, Dropdown, Menu, Modal, Space, Typography } from "antd";
import { AppstoreAddOutlined, ExclamationCircleOutlined } from "@ant-design/icons";
import { Loader, useApi } from "micser-common";

import PageContainer from "/components/PageContainer";
import Widget, { WidgetTypesContext } from "./Widget";

import { useErrorNotification } from "/hooks";
import { PluginsContext } from "/hooks/usePlugins";
import { getRelativeCoordinates } from "/utils";

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

const Dashboard = () => {
    const plugins = useContext(PluginsContext);
    const [elements, setElements] = useState([]);
    const [isAddDrawerOpen, setIsAddDrawerOpen] = useState(false);
    const [flowTransform, setFlowTransform] = useState({ x: 0, y: 0, zoom: 1 });
    const [contextMenuTarget, setContextMenuTarget] = useState();

    const modulesApi = useApi("Modules");
    const { result: modules } = modulesApi;

    const moduleConnectionsApi = useApi("ModuleConnections");
    const { result: moduleConnections } = moduleConnectionsApi;

    const moduleDescriptionsApi = useApi("Modules/Descriptions");
    const { result: moduleDescriptions } = moduleDescriptionsApi;

    useErrorNotification([modulesApi.error, moduleConnectionsApi.error, moduleDescriptionsApi.error]);

    const widgetTypes = useMemo(() => {
        return plugins.reduce((prev, curr) => prev.concat(curr.widgets), []);
    }, [plugins]);

    useEffect(() => {
        if (modules && moduleConnections) {
            const moduleElements = modules.map((dto) => ({
                id: String(dto.id),
                type: "Widget",
                position: { x: Number(dto.state.left), y: Number(dto.state.top) },
                data: dto,
            }));
            const connectionElements = moduleConnections.map((dto) => ({
                id: String(dto.id),
                source: String(dto.sourceId),
                target: String(dto.targetId),
                sourceHandle: dto.sourceConnectorName,
                targetHandle: dto.targetConnectorName,
                animated: true,
                arrowHeadType: "arrow",
            }));

            setElements(moduleElements.concat(connectionElements));
        }
    }, [modules, moduleConnections]);

    const nodeTypes = useMemo(() => ({ Widget: Widget }), []);

    const deleteAllModules = useCallback(async () => {
        await modulesApi.load({ method: "delete" });
        modulesApi.load({ setResult: true });
    }, [modulesApi]);

    const handleDragStart = (e, moduleDescription) => {
        e.dataTransfer.setData(ModuleDescriptionKey, JSON.stringify(moduleDescription));
        const offset = getRelativeCoordinates(e);
        e.dataTransfer.setData(DragOffsetKey, JSON.stringify(offset));
    };

    const handleDragOver = (e) => {
        if (e.dataTransfer.types.includes(ModuleDescriptionKey)) {
            e.preventDefault();
        }
    };

    const handleDrop = async (e) => {
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
                moduleType: moduleDescription.name,
                state: {
                    left: position.x - flowTransform.x,
                    top: position.y - flowTransform.y,
                },
            };

            await modulesApi.load({ method: "post", data: module });
            modulesApi.load({ setResult: true });
        }
    };

    const handleNodeDragStop = (_, node) => {
        const { data: module } = node;
        const { x, y } = node.position;
        module.state.left = x;
        module.state.top = y;

        modulesApi.load({ method: "put", data: module });
    };

    const handleMoveEnd = (transform) => {
        setFlowTransform(transform);
    };

    const handleContextMenu = (_, node) => {
        setContextMenuTarget(node);
    };

    const isLoading = modulesApi.isLoading || moduleConnectionsApi.isLoading || moduleDescriptionsApi.isLoading;

    const contextMenu = useMemo(() => {
        const handleContextMenuClick = ({ key }) => {
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

    return (
        <PageContainer noPadding>
            {isLoading && <Loader />}
            <WidgetTypesContext.Provider value={widgetTypes}>
                <Dropdown overlay={contextMenu} trigger={["contextMenu"]}>
                    <ReactFlow
                        elements={elements}
                        nodeTypes={nodeTypes}
                        snapToGrid
                        snapGrid={[GridSize, GridSize]}
                        defaultZoom={flowTransform.zoom}
                        defaultPosition={[flowTransform.x, flowTransform.y]}
                        style={{ width: "100%", height: "100%" }}
                        onDragOver={handleDragOver}
                        onDrop={handleDrop}
                        onNodeDragStop={handleNodeDragStop}
                        onlyRenderVisibleElements={false}
                        onMoveEnd={handleMoveEnd}
                        onPaneContextMenu={handleContextMenu}
                        onNodeContextMenu={handleContextMenu}
                    >
                        <Controls />
                        <Background variant="dots" size={1} gap={10} color="#333" />
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

export default Dashboard;
