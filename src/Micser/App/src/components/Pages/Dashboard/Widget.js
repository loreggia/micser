import React, { createContext, useContext, useMemo } from "react";
import { Card } from "antd";
import { Handle } from "react-flow-renderer";
import styled from "styled-components";

const WidgetTypesContext = createContext([]);

const ErrorContent = ({ data }) => {
    return <>Widget type '{data.widgetType}' not found.</>;
};

const WidgetCard = styled(Card)`
    cursor: inherit;
    box-shadow: inset 0 0 5px 0 rgba(255, 255, 255, 0.5);
`;

const Widget = ({ data }) => {
    const widgetTypes = useContext(WidgetTypesContext);
    const type = useMemo(() => widgetTypes.find((t) => t.name === data.widgetType) || {}, [data, widgetTypes]);
    const Content = type.content || ErrorContent;

    return (
        <WidgetCard size="small" title={data.state.title || data.widgetType} hoverable>
            <Content data={data} />
            {type.inputHandles &&
                type.inputHandles.map((name) => <Handle key={name} id={name} type="target" position="left" />)}
            {type.outputHandles &&
                type.outputHandles.map((name) => <Handle key={name} id={name} type="source" position="right" />)}
        </WidgetCard>
    );
};

export { WidgetTypesContext };
export default Widget;
