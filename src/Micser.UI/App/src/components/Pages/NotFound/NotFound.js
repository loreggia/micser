import React from "react";
import { useTranslation } from "react-i18next";

import Alert from "../../Alert";
import Section from "../../Section";

const NotFound = () => {
    const { t } = useTranslation();

    return (
        <Section>
            <Alert severity="error">{t("notFound.message")}</Alert>
        </Section>
    );
};

export default NotFound;
