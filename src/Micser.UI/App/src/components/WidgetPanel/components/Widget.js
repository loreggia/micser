import React from "react";
import styled from "styled-components";

import { WidgetType } from "../propTypes";

const Container = styled.div`
    position: absolute;
    display: flex;
    flex-direction: column;
    top: ${(p) => p.bounds.top}px;
    left: ${(p) => p.bounds.left}px;
    width: ${(p) => p.bounds.width}px;
    height: ${(p) => p.bounds.height}px;
`;

const Header = styled.div`
    flex: 0 0 auto;
`;

const Content = styled.div`
    flex: 1 1 auto;
`;

const Widget = ({ className, bounds, header, children }) => {
    return (
        <Container className={className} bounds={bounds}>
            <Header>{header}</Header>
            <Content>{children}</Content>
        </Container>
    );
};

Widget.propTypes = WidgetType;

export default Widget;
