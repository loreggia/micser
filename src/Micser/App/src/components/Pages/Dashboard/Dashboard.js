import React, { useContext, useEffect, useMemo, useState } from "react";
import styled from "styled-components";
import ReactFlow, { Background, Controls } from "react-flow-renderer";
import { Card, Drawer, Space, Typography } from "antd";
import { AppstoreAddOutlined } from "@ant-design/icons";
import { Loader, useApi } from "micser-common";

import PageContainer from "/components/PageContainer";
import Widget, { WidgetTypesContext } from "./Widget";

import { useErrorNotification } from "/hooks";
import { PluginsContext } from "/hooks/usePlugins";

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

const DragDropKey = "moduledescription";

const Dashboard = () => {
    const plugins = useContext(PluginsContext);
    const [elements, setElements] = useState([]);
    const [isAddDrawerOpen, setIsAddDrawerOpen] = useState(false);
    const [newModule, setNewModule] = useState(null);

    const [modules, isLoadingModules, modulesError, refreshModules] = useApi("Modules");
    const [moduleConnections, isLoadingModuleConnections, moduleConnectionsError] = useApi("ModuleConnections");
    const [moduleDescriptions, isLoadingModuleDescriptions, moduleDescriptionsError] = useApi("Modules/Descriptions");
    const [addedModule, isAddingModule, addError, addModule] = useApi("Modules", {
        autoLoad: false,
        method: "post",
        data: newModule,
    });

    useErrorNotification([modulesError, moduleConnectionsError, moduleDescriptionsError, addError]);

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

    useEffect(() => {
        if (addedModule) {
            refreshModules();
        }
    }, [refreshModules, addedModule]);

    const nodeTypes = useMemo(() => ({ Widget: Widget }), []);

    const handleDragStart = (e, moduleDescription) => {
        e.dataTransfer.setData(DragDropKey, JSON.stringify(moduleDescription));
    };

    const handleDragOver = (e) => {
        if (e.dataTransfer.types.includes(DragDropKey)) {
            e.preventDefault();
        }
    };

    const handleDrop = (e) => {
        const data = e.dataTransfer.getData(DragDropKey);
        if (data) {
            const moduleDescription = JSON.parse(data);
            const { clientX, clientY } = e;

            const module = {
                moduleType: moduleDescription.name,
                state: {
                    left: clientX,
                    top: clientY,
                },
            };

            console.log(module);

            setNewModule(module);
            addModule();
        }
    };

    const handleNodeDragStop = (e, node) => {};

    const isLoading = isLoadingModules || isLoadingModuleConnections || isLoadingModuleDescriptions || isAddingModule;

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
                    onNodeDragStop={handleNodeDragStop}
                >
                    <Controls />
                    <Background variant="dots" size={1} gap={10} color="#333" />
                </ReactFlow>
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
