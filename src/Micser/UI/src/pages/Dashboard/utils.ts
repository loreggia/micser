import { useCallback } from "react";
import { useTranslation } from "react-i18next";
import { Module, Widget } from "micser-common";

export interface WidgetType {
    type?: Widget;
    title: string;
}

type GetWidgetTypeFunc = (module?: Module) => WidgetType;

export const useGetWidgetType = (widgetTypes: Widget[]): GetWidgetTypeFunc => {
    const { t } = useTranslation();

    const getWidgetType = useCallback(
        (module?: Module): WidgetType => {
            const type = widgetTypes.find((t) => t.name === module?.type);
            const title = (type && t(type.titleResource)) || module?.type || "Error";
            return type ? { type, title } : { type: undefined, title };
        },
        [widgetTypes, t]
    );

    return getWidgetType;
};
