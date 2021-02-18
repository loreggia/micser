import React, { FC } from "react";
import styled from "styled-components";
import { Spin } from "antd";
import { useTranslation } from "react-i18next";

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
    isVisible: boolean;
}

export const Loader: FC<LoaderProps> = ({ isVisible = true }) => {
    const { t } = useTranslation();

    return isVisible ? (
        <Container>
            <Spin tip={t("global.loading")} />
        </Container>
    ) : null;
};
