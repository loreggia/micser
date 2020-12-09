import PropTypes from "prop-types";

export const BoundsType = PropTypes.shape({
    top: PropTypes.number.isRequired,
    left: PropTypes.number.isRequired,
    width: PropTypes.number.isRequired,
    height: PropTypes.number.isRequired,
});

export const WidgetType = PropTypes.shape({
    bounds: PropTypes.shape(BoundsType).isRequired,
    header: PropTypes.oneOfType([PropTypes.string, PropTypes.element]),
});
