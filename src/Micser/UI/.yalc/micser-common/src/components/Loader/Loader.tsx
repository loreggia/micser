import React, { FC } from "react";
import styled from "styled-components";
import { Spin } from "antd";

const Container = styled.div`
    position: absolute;
    width: 100%;
    height: 100%;
    display: flex;
    justify-content: center;
    align-items: center;
    background-color: rgba(0, 0, 0, 0.5);
    z-index: 999;
`;

export interface LoaderProps {
    isVisible?: boolean;
    tip?: string;
}

export const Loader: FC<LoaderProps> = ({ isVisible, tip }) => {
    return isVisible ? (
        <Container>
            <Spin tip={tip} />
        </Container>
    ) : null;
};

Loader.defaultProps = {
    isVisible: true,
    tip: "Loading...",
};
