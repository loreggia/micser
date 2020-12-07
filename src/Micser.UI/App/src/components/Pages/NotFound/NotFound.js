import React from "react";
import { useTranslation } from "react-i18next";

import Alert from "../../Alert";

const NotFound = () => {
    const { t } = useTranslation();

    return <Alert color="error">{t("notFound.message")}</Alert>;
};

export default NotFound;
