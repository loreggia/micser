import React, { Children, useCallback, useLayoutEffect, useState } from "react";
import PropTypes from "prop-types";
import styled from "styled-components";
import { Widget } from "./components";
import { useDragging } from "./hooks";

const Container = styled.div`
    position: relative;
    width: 100%;
    height: 100%;
    overflow: auto;
`;

const getLayoutItem = (layout, id) => {
    return layout.find((l) => String(l.id) === String(id));
};

const WidgetPanel = ({ layout, onLayoutChanged, children }) => {
    const [draggingWidget, setDraggingWidget] = useState();

    const updateLayout = useCallback(() => {
        if (draggingWidget) {
            const layoutItem = getLayoutItem(layout, draggingWidget.id);
            const newLayout = layout.map((item) => {
                const newItem = { ...item };
                if (item === layoutItem) {
                    newItem.bounds = {
                        ...newItem.bounds,
                        top: draggingWidget.offsetTop,
                        left: draggingWidget.offsetLeft,
                    };
                }
                return newItem;
            });
            onLayoutChanged(newLayout);
        }
    }, [draggingWidget, layout, onLayoutChanged]);

    const onDragStart = useCallback((element) => {
        setDraggingWidget(element);
    }, []);
    const onDrag = useCallback(() => {}, []);
    const onDragEnd = useCallback(() => {
        updateLayout();
        setDraggingWidget();
    }, [updateLayout]);

    const registerDragElement = useDragging({ onDragStart, onDrag, onDragEnd });

    useLayoutEffect(() => {}, [layout]);

    const renderChild = (child) => {
        if (!child || !child.key) {
            return;
        }

        const layoutItem = getLayoutItem(layout, child.key);

        if (!layoutItem) {
            return null;
        }

        return (
            <Widget elementRef={registerDragElement} id={layoutItem.id} bounds={layoutItem.bounds}>
                {child}
            </Widget>
        );
    };

    return <Container>{Children.map(children, renderChild)}</Container>;
};

WidgetPanel.propTypes = {
    layout: PropTypes.arrayOf(PropTypes.shape({})).isRequired,
    onLayoutChanged: PropTypes.func,
    children: PropTypes.any,
};

export default WidgetPanel;
