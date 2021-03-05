import React from "react";
import { notification } from "antd";
import { Trans } from "react-i18next";

export type ErrorType = { message?: string; messageId?: string };

export const showError = (error?: ErrorType): void => {
    if (error) {
        let description: React.ReactNode = error.message;

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

interface Coordinate {
    x: number;
    y: number;
}

export const getRelativeCoordinates = (event: React.MouseEvent, element: Nullable<HTMLElement> = null): Coordinate => {
    if (!element) {
        element = event.target as HTMLElement;
    }

    const position = {
        x: event.pageX,
        y: event.pageY,
    };

    const offset = {
        left: element.offsetLeft,
        top: element.offsetTop,
    };

    let parent = element.offsetParent as HTMLElement;

    while (parent) {
        offset.left += parent.offsetLeft;
        offset.top += parent.offsetTop;
        parent = parent.offsetParent as HTMLElement;
    }

    return {
        x: position.x - offset.left,
        y: position.y - offset.top,
    };
};