import { notification } from "antd";

export const showError = (error) => {
    if (error) {
        notification.open({
            message: "Error",
            description: error.message || error,
            type: "error",
        });
    }
};

export const getRelativeCoordinates = (event, element = null) => {
    if (!element) {
        element = event.target;
    }

    const position = {
        x: event.pageX,
        y: event.pageY,
    };

    const offset = {
        left: element.offsetLeft,
        top: element.offsetTop,
    };

    let parent = element.offsetParent;

    while (parent) {
        offset.left += parent.offsetLeft;
        offset.top += parent.offsetTop;
        parent = parent.offsetParent;
    }

    return {
        x: position.x - offset.left,
        y: position.y - offset.top,
    };
};
