import React from "react";
import styled from "styled-components";
import { Spin } from "antd";

const Container = styled.div`
    width: 100%;
    height: 100%;
    display: flex;
    justify-content: center;
    align-items: center;
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
