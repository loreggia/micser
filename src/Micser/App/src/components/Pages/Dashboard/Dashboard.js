import React, { useContext, useEffect, useMemo, useState } from "react";
import ReactFlow, { Background, Controls } from "react-flow-renderer";
import { Loader, useApi } from "micser-common";

import PageContainer from "../../PageContainer";

import { PluginsContext } from "../../../hooks/usePlugins";
import Widget, { WidgetTypesContext } from "./Widget";
import { Card, Drawer, Menu, notification, Space, Typography } from "antd";
import { AppstoreAddOutlined } from "@ant-design/icons";
import styled from "styled-components";

const showError = (error) => {
    if (error) {
        notification.open({
            message: "Error",
            description: error.message || error,
            type: "error",
        });
    }
};

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

const Dashboard = () => {
    const plugins = useContext(PluginsContext);
    const [elements, setElements] = useState([]);
    const [isAddDrawerOpen, setIsAddDrawerOpen] = useState(false);

    const [modules, isLoadingModules, refreshModules, modulesLoaded, modulesError] = useApi("Modules");
    const [
        moduleConnections,
        isLoadingModuleConnections,
        refreshModuleConnections,
        moduleConnectionsLoaded,
        moduleConnectionsError,
    ] = useApi("ModuleConnections");
    const [widgets, isLoadingWidgets, refreshWidgets, widgetsLoaded, widgetsError] = useApi("Widgets");

    useEffect(() => {
        showError(modulesError);
        showError(moduleConnectionsError);
        showError(widgetsError);
    }, [modulesError, moduleConnectionsError, widgetsError]);

    const widgetTypes = useMemo(() => {
        return plugins.reduce((prev, curr) => prev.concat(curr.widgets), []);
    }, [plugins]);

    useEffect(() => {
        if (modules && moduleConnections) {
            const moduleElements = modules.map((dto) => ({
                id: String(dto.id),
                type: "Widget",
                position: { x: dto.state.left, y: dto.state.top },
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

            // setElements(moduleElements.concat(connectionElements));
            setElements([
                {
                    id: "1",
                    type: "Widget",
                    position: { x: 10, y: 10 },
                    data: {
                        id: 1,
                        widgetType: "DeviceInput",
                        state: {},
                    },
                },
                {
                    id: "2",
                    type: "Widget",
                    position: { x: 200, y: 50 },
                    data: {
                        id: 2,
                        widgetType: "DeviceOutput",
                        state: {},
                    },
                },
            ]);
        }
    }, [modules, moduleConnections]);

    const nodeTypes = useMemo(() => ({ Widget: Widget }), []);

    const handleDragStart = (e, widget) => {
        e.dataTransfer.setData("widget", JSON.stringify(widget));
    };

    const handleDragOver = (e) => {
        if (e.dataTransfer.types.includes("widget")) {
            e.preventDefault();
        }
    };

    const handleDrop = (e) => {
        const strWidget = e.dataTransfer.getData("widget");
        if (strWidget) {
            const widget = JSON.parse(strWidget);
            const { clientX, clientY } = e;

            const module = {
                widgetType: widget.name,
                state: {
                    left: clientX,
                    top: clientY,
                },
            };
        }
    };

    const isLoading = isLoadingModules || isLoadingModuleConnections || isLoadingWidgets;

    return (
        <PageContainer noPadding>
            {isLoading && <Loader />}
            <WidgetTypesContext.Provider value={widgetTypes}>
                <ReactFlow
                    elements={elements}
                    nodeTypes={nodeTypes}
                    snapToGrid
                    snapGrid={[10, 10]}
                    style={{ width: "100%", height: "100%" }}
                    onDragOver={handleDragOver}
                    onDrop={handleDrop}
                >
                    <Controls />
                    <Background variant="dots" size={1} gap={10} color="#333" />
                </ReactFlow>
                <Drawer visible={isAddDrawerOpen} onClose={() => setIsAddDrawerOpen(false)} width="300px" mask={false}>
                    <Title level={2}>Add Widget</Title>
                    <Text type="secondary">Drag a widget from the list to the dashboard.</Text>
                    <Space direction="vertical" size="middle" style={{ width: "100%" }}>
                        {widgets &&
                            widgets.map((widget) => (
                                <WidgetListCard
                                    key={widget.name}
                                    size="small"
                                    title={widget.title}
                                    hoverable
                                    draggable
                                    onDragStart={(e) => handleDragStart(e, widget)}
                                >
                                    {widget.description}
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
