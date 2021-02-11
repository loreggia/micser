import React, { createContext, ReactNode, useContext, useMemo } from "react";
import { Card } from "antd";
import { Handle } from "react-flow-renderer";
import styled from "styled-components";

export interface WidgetType {
    name: string;
    content: ReactNode;
    inputHandles?: string[];
    outputHandles?: string[];
}

export const WidgetTypesContext = createContext<WidgetType[]>([]);

const ErrorContent = ({ data }) => {
    return <>Widget for module type '{data.type}' not found.</>;
};

const WidgetCard = styled(Card)`
    cursor: inherit;
    box-shadow: inset 0 0 5px 0 rgba(255, 255, 255, 0.5);
`;

export interface WidgetProps {
    data: ModuleDto;
}

export const Widget = ({ data }) => {
    const widgetTypes = useContext(WidgetTypesContext);
    const type = useMemo(() => widgetTypes.find((t) => t.name === data.type) || {}, [data, widgetTypes]);
    const Content = type.content || ErrorContent;

    return (
        <WidgetCard size="small" title={data.state.title || data.type} hoverable>
            <Content data={data} />
            {type.inputHandles &&
                type.inputHandles.map((name) => <Handle key={name} id={name} type="target" position="left" />)}
            {type.outputHandles &&
                type.outputHandles.map((name) => <Handle key={name} id={name} type="source" position="right" />)}
        </WidgetCard>
    );
};
