import React from "react";
import { useTranslation } from "react-i18next";
import { Alert } from "antd";
import PageContainer from "/components/PageContainer";

const NotFound = () => {
    const { t } = useTranslation();

    return (
        <PageContainer>
            <Alert type="error" message="Error" description={t("notFound.message")} showIcon />
        </PageContainer>
    );
};

export default NotFound;
