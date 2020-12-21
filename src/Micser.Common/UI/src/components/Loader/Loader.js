import React from "react";
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

const Loader = ({ tip }) => {
    return (
        <Container>
            <Spin tip={tip} />
        </Container>
    );
};

Loader.defaultProps = {
    tip: "Loading...",
};

export default Loader;
