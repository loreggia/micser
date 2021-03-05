import React, { FC, useContext } from "react";
import { Card } from "antd";
import { Handle, Position } from "react-flow-renderer";
import styled from "styled-components";
import { Contexts, Module, WidgetFC, WidgetProps } from "micser-common";
import { useGetWidgetType } from "./utils";

const ErrorContent: WidgetFC = ({ module }: WidgetProps) => {
    return <>Widget for module type &apos;{module?.type}&apos; not found.</>;
};

const WidgetCard = styled(Card)`
    cursor: inherit;
    box-shadow: inset 0 0 5px 0 rgba(255, 255, 255, 0.5);
    min-width: 340px;
    max-width: 500px;
`;

interface WidgetNodeProps {
    data: Module;
}

export const Widget: FC<WidgetNodeProps> = ({ data }: WidgetNodeProps) => {
    const widgetTypes = useContext(Contexts.widgetTypes);
    const getWidgetType = useGetWidgetType(widgetTypes);
    const { type, title } = getWidgetType(data);

    const Content: WidgetFC = (type && type.content) || ErrorContent;

    return (
        <WidgetCard size="small" title={title} hoverable>
            <Content module={data} />
            {type?.inputHandles &&
                type.inputHandles.map((name) => <Handle key={name} id={name} type="target" position={Position.Left} />)}
            {type?.outputHandles &&
                type.outputHandles.map((name) => (
                    <Handle key={name} id={name} type="source" position={Position.Right} />
                ))}
        </WidgetCard>
    );
};
