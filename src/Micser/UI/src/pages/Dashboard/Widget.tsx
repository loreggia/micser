import React, { createContext, useContext, useMemo } from "react";
import { Card } from "antd";
import { Handle, Position } from "react-flow-renderer";
import styled from "styled-components";
import { Module, WidgetContent, WidgetType } from "micser-common";

export const WidgetTypesContext = createContext<WidgetType[]>([]);

const ErrorContent = ({ data }: { data: Module }) => {
    return <>Widget for module type '{data?.type}' not found.</>;
};

const WidgetCard = styled(Card)`
    cursor: inherit;
    box-shadow: inset 0 0 5px 0 rgba(255, 255, 255, 0.5);
`;

export const Widget = ({ data }: { data: Module }) => {
    const widgetTypes = useContext(WidgetTypesContext);
    const type = useMemo<WidgetType | undefined>(() => widgetTypes.find((t) => t.name === data.type), [
        data,
        widgetTypes,
    ]);
    const Content: WidgetContent = (type && type.content) || ErrorContent;

    return (
        <WidgetCard size="small" title={data.state.title || data.type} hoverable>
            <Content data={data} />
            {type?.inputHandles &&
                type.inputHandles.map((name) => <Handle key={name} id={name} type="target" position={Position.Left} />)}
            {type?.outputHandles &&
                type.outputHandles.map((name) => (
                    <Handle key={name} id={name} type="source" position={Position.Right} />
                ))}
        </WidgetCard>
    );
};
