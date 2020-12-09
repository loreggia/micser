import React, { Children, useCallback, useEffect, useLayoutEffect, useState } from "react";
import PropTypes from "prop-types";
import styled from "styled-components";
import { Widget } from "./components";

const Container = styled.div`
    position: relative;
    width: 100%;
    height: 100%;
`;

const getLayoutItem = (layout, id) => {
    return layout.find((l) => String(l.id) === String(id));
};

const WidgetPanel = ({ layout, onLayoutChanged, children }) => {
    const [dragStart, setDragStart] = useState();
    const [origPos, setOrigPos] = useState();
    const [draggingWidget, setDraggingWidget] = useState();

    const updateLayout = useCallback(() => {
        if (draggingWidget) {
            const layoutItem = getLayoutItem(layout, draggingWidget.id);
            const newLayout = layout.map((item) => {
                const newItem = { ...item };
                if (item === layoutItem) {
                    newItem.bounds = {
                        ...newItem.bounds,
                        top: Number(draggingWidget.style.top.replace(/[a-z]+/, "")),
                        left: Number(draggingWidget.style.left.replace(/[a-z]+/, "")),
                    };
                }
                return newItem;
            });
            onLayoutChanged(newLayout);
        }
    }, [draggingWidget, layout, onLayoutChanged]);

    const handleMouseMove = useCallback(
        (e) => {
            if (!draggingWidget) {
                return;
            }
            const { clientX, clientY } = e;

            draggingWidget.style.left = origPos.left - (dragStart.left - clientX) + "px";
            draggingWidget.style.top = origPos.top - (dragStart.top - clientY) + "px";
        },
        [dragStart, draggingWidget, origPos]
    );

    const handleMouseUp = useCallback(() => {
        updateLayout();
        setDraggingWidget();
    }, [updateLayout]);

    const handleMouseDown = useCallback(
        (e) => {
            const widget = e.currentTarget;

            if (!widget) {
                return;
            }

            e.preventDefault();
            setDraggingWidget(widget);
            const layoutItem = getLayoutItem(layout, widget.id);
            setOrigPos({ left: layoutItem.bounds.left, top: layoutItem.bounds.top });
            setDragStart({ left: e.clientX, top: e.clientY });
        },
        [layout]
    );

    useEffect(() => {
        window.addEventListener("mousemove", handleMouseMove);
        window.addEventListener("mouseup", handleMouseUp);

        return () => {
            window.removeEventListener("mousemove", handleMouseMove);
            window.removeEventListener("mouseup", handleMouseUp);
        };
    }, [handleMouseMove, handleMouseUp]);

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
            <Widget id={layoutItem.id} bounds={layoutItem.bounds} onMouseDown={handleMouseDown}>
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
