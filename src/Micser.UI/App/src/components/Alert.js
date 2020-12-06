import React from "react";
import styled from "styled-components";

const Container = styled.div`
    padding: 20px;
    background-color: red;
    color: white;
    font-size: 1.2em;
`;

const Alert = ({ children }) => {
    return <Container>{children}</Container>;
};

export default Alert;
