import React, { useContext, useEffect, useMemo, useState } from "react";
import ReactFlow, { Background, Controls } from "react-flow-renderer";

import PageContainer from "../../PageContainer";
import Loader from "../../Loader";

import { useApi } from "../../../hooks";
import { PluginsContext } from "../../../hooks/usePlugins";
import Widget, { WidgetTypesContext } from "./Widget";

const Dashboard = () => {
    const plugins = useContext(PluginsContext);
    const [elements, setElements] = useState([]);
    const [modules, isLoadingModules] = useApi("Modules");
    const [moduleConnections, isLoadingModuleConnections] = useApi("ModuleConnections");

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

    const isLoading = isLoadingModules || isLoadingModuleConnections;

    return (
        <PageContainer noPadding>
            {isLoading && <Loader />}
            <WidgetTypesContext.Provider value={widgetTypes}>
                <ReactFlow
                    elements={elements}
                    nodeTypes={nodeTypes}
                    snapToGrid
                    snapGrid={[10, 10]}
                    style={{ width: "100%", height: "100vh" }}
                >
                    <Controls />
                    <Background variant="dots" size={1} gap={10} color="#333" />
                </ReactFlow>
            </WidgetTypesContext.Provider>
        </PageContainer>
    );
};

export default Dashboard;
