import PropTypes from "prop-types";
import styled from "styled-components";

const PageContainer = styled.div`
    position: relative;
    width: 100%;
    height: 100%;
    padding: ${(p) => (p.noPadding ? "0" : "20px")};
`;

PageContainer.defaultProps = {
    noPadding: false,
};

PageContainer.propTypes = {
    noPadding: PropTypes.bool.isRequired,
};

export default PageContainer;
