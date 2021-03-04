import React, { FC } from "react";
import { useTranslation } from "react-i18next";
import { Alert } from "antd";

import { PageContainer } from "~/components";

export const NotFound: FC = () => {
    const { t } = useTranslation();

    return (
        <PageContainer>
            <Alert type="error" message="Error" description={t("notFound.message")} showIcon />
        </PageContainer>
    );
};
