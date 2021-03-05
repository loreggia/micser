import React, { FC, useEffect, useState } from "react";
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
    suspenseTime?: number;
}

export const Loader: FC<LoaderProps> = ({ isVisible, tip, suspenseTime }: LoaderProps) => {
    const [isVisibleInternal, setIsVisibleInternal] = useState(false);

    useEffect(() => {
        let timeout: number;

        if (isVisible) {
            const handler: TimerHandler = () => {
                setIsVisibleInternal(true);
            };
            timeout = setTimeout(handler, suspenseTime);
        } else {
            clearTimeout(timeout);
            setIsVisibleInternal(false);
        }

        return () => {
            clearTimeout(timeout);
        };
    }, [isVisible, suspenseTime]);

    return isVisibleInternal ? (
        <Container>
            <Spin tip={tip} />
        </Container>
    ) : null;
};

Loader.defaultProps = {
    isVisible: true,
    tip: "Loading...",
    suspenseTime: 1000,
};
