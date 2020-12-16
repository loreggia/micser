import React from "react";
import PropTypes from "prop-types";
import styled from "styled-components";

import { BoundsType } from "../propTypes";

const WidgetContainer = styled.div`
    position: absolute;
    top: ${(p) => p.top}px;
    left: ${(p) => p.left}px;
    width: ${(p) => p.width}px;
    height: ${(p) => p.height}px;

    & > * {
        width: 100%;
        height: 100%;
    }
`;

const Widget = ({ elementRef, id, bounds, onMouseDown, children }) => {
    return (
        <WidgetContainer
            ref={elementRef}
            id={id}
            top={bounds.top}
            left={bounds.left}
            width={bounds.width}
            height={bounds.height}
            onMouseDown={onMouseDown}
        >
            {children}
        </WidgetContainer>
    );
};

Widget.propTypes = {
    id: PropTypes.any.isRequired,
    bounds: BoundsType.isRequired,
    children: PropTypes.any,
};

export default Widget;
