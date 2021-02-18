import React, { createContext, useContext, useMemo } from "react";
import { Card } from "antd";
import { useTranslation } from "react-i18next";
import { Handle, Position } from "react-flow-renderer";
import styled from "styled-components";
import { Module, Widget as WidgetType, WidgetFC } from "micser-common";

export const WidgetTypesContext = createContext<WidgetType[]>([]);

const ErrorContent: WidgetFC = ({ module }) => {
    return <>Widget for module type '{module?.type}' not found.</>;
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

    const { t } = useTranslation();

    const Content: WidgetFC = (type && type.content) || ErrorContent;

    return (
        <WidgetCard size="small" title={(type && t(type.titleResource)) || data.type} hoverable>
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
