import React, { useCallback, useEffect, useState } from "react";
import ReactFlow, { Background, Controls } from "react-flow-renderer";
import { useApi } from "../../../hooks";
import Loader from "../../Loader";
import PageContainer from "../../PageContainer";

const Dashboard = () => {
    const [elements, setElements] = useState([]);
    const [modulesApi, isLoadingModules] = useApi("Modules");
    const [moduleConnectionsApi, isLoadingModuleConnections] = useApi("ModuleConnections");

    const loadData = useCallback(async () => {
        const pModules = modulesApi("/");
        const pModuleConnections = moduleConnectionsApi("/");
        const [modules, moduleConnections] = await Promise.all([pModules, pModuleConnections]);

        if (modules.data && moduleConnections.data) {
            const moduleElements = modules.data.map((dto) => ({
                id: String(dto.id),
                type: dto.widgetType,
                position: { x: dto.state.left, y: dto.state.top },
                data: dto.state,
            }));
            const connectionElements = moduleConnections.data.map((dto) => ({
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
    }, [modulesApi, moduleConnectionsApi]);

    // todo
    const nodeTypes = [];

    useEffect(() => {
        // loadData();
    }, [loadData]);

    const isLoading = isLoadingModules || isLoadingModuleConnections;

    return (
        <PageContainer noPadding>
            {isLoading ? (
                <Loader />
            ) : (
                <ReactFlow elements={elements} nodeTypes={nodeTypes} snapToGrid snapGrid={[10, 10]}>
                    <Controls />
                    <Background variant="dots" size={1} gap={10} color="#333" />
                </ReactFlow>
            )}
        </PageContainer>
    );
};

export default Dashboard;
