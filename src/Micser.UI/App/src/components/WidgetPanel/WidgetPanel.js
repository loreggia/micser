import React from "react";
import PropTypes from "prop-types";
import Widget from "./components";

const Container = styled.div`
    position: relative;
    width: 100%;
    height: 100%;
`;

const WidgetPanel = ({ widgets }) => {
    return (
        <Container>
            {widgets.map((widget) => (
                <Widget {...widget} />
            ))}
        </Container>
    );
};

WidgetPanel.propTypes = {
    widgets: PropTypes.arrayOf(PropTypes.shape(WidgetType)),
};

WidgetPanel.defaultProps = {
    widgets: [],
};

export default WidgetPanel;
