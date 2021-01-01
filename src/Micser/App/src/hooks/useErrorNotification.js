import { useEffect } from "react";
import { showError } from "/utils";

const useErrorNotification = (errors) => {
    useEffect(() => {
        errors.forEach(showError);
        // eslint-disable-next-line
    }, errors);
};

export default useErrorNotification;
