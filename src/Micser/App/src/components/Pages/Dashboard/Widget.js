import React, { createContext, useContext, useMemo } from "react";
import { Card } from "antd";
import { Handle } from "react-flow-renderer";

const WidgetTypesContext = createContext([]);

const ErrorContent = ({ data }) => {
    return <>Widget type '{data.widgetType}' not found.</>;
};

const Widget = ({ data }) => {
    const widgetTypes = useContext(WidgetTypesContext);
    const type = useMemo(() => widgetTypes.find((t) => t.name === data.widgetType) || {}, [data, widgetTypes]);
    const Content = type.content || ErrorContent;

    return (
        <Card size="small" title={data.state.title || data.widgetType}>
            <Content data={data} />
            {type.inputHandles &&
                type.inputHandles.map((name) => <Handle key={name} id={name} type="target" position="left" />)}
            {type.outputHandles &&
                type.outputHandles.map((name) => <Handle key={name} id={name} type="source" position="right" />)}
        </Card>
    );
};

export { WidgetTypesContext };
export default Widget;
