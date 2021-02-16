import { useEffect } from "react";
import { ErrorType, showError } from "~/utils";

export const useErrorNotification = (errors: ErrorType[]) => {
    useEffect(() => {
        errors.forEach(showError);
        // eslint-disable-next-line
    }, errors);
};
