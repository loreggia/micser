import styled from "styled-components";

export interface PageContainerProps {
    noPadding?: boolean;
}

export const PageContainer = styled.div<PageContainerProps>`
    position: relative;
    width: 100%;
    height: 100%;
    padding: ${(p) => (p.noPadding ? "0" : "20px")};
`;

PageContainer.defaultProps = {
    noPadding: false,
};
