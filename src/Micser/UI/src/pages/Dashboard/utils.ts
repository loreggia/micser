import { useCallback } from "react";
import { useTranslation } from "react-i18next";
import { Module, Widget } from "micser-common";

export const useGetWidgetType = (widgetTypes: Widget[]) => {
    const { t } = useTranslation();

    const getWidgetType = useCallback(
        (module?: Module) => {
            const type = widgetTypes.find((t) => t.name === module?.type);
            const title = (type && t(type.titleResource)) || module?.type || "Error";

            return {
                type,
                title,
            };
        },
        [widgetTypes]
    );

    return getWidgetType;
};
