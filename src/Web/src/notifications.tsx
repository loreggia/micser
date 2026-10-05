import { Toast, ToastBody, ToastTitle, useToastController } from "@fluentui/react-components";
import { EngineApiError, useEngineConnection } from "@micser/web-sdk";
import { useCallback, useEffect } from "react";
import { useTranslation } from "./i18n";

export const toasterId = "notifications";

/**
 * Returns a function that shows an error notification.
 */
export function useNotifyError() {
  const { dispatchToast } = useToastController(toasterId);

  return useCallback(
    (title: string, error: unknown) => {
      const details =
        error instanceof EngineApiError && Object.keys(error.errors).length > 0
          ? Object.entries(error.errors)
              .map(([path, messages]) => `${path}: ${messages.join(" ")}`)
              .join("\n")
          : error instanceof Error
            ? error.message
            : String(error);

      dispatchToast(
        <Toast>
          <ToastTitle>{title}</ToastTitle>
          <ToastBody>{details}</ToastBody>
        </Toast>,
        { intent: "error" }
      );
    },
    [dispatchToast]
  );
}

/**
 * Shows notifications for failed module updates.
 */
export function useErrorNotifications() {
  const connection = useEngineConnection();
  const notifyError = useNotifyError();
  const { t } = useTranslation();

  useEffect(
    () => connection.onError((error) => notifyError(t("module.updateFailed"), error)),
    [connection, notifyError, t]
  );
}
