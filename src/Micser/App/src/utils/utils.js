import { notification } from "antd";
import { Translation } from "react-i18next";
import { Trans } from "react-i18next";

export const showError = (error) => {
    if (error) {
        let description = error.message;

        if (error.messageId) {
            description = <Trans i18nKey={error.messageId}>Unknown error</Trans>;
        }

        notification.open({
            message: "Error",
            description: description || error,
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
