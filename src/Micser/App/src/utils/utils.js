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
