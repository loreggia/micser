import PropTypes from "prop-types";

export const BoundsType = {
    top: PropTypes.number.isRequired,
    left: PropTypes.number.isRequired,
    width: PropTypes.number.isRequired,
    height: PropTypes.number.isRequired,
};

export const WidgetType = {
    className: PropTypes.string,
    bounds: PropTypes.shape(BoundsType).isRequired,
    header: PropTypes.any(PropTypes.string, PropTypes.element),
};
