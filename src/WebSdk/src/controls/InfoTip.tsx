import { InfoButton } from "@fluentui/react-components";
import { useTranslation } from "../i18n/translations";
import { useShowHelp } from "./showHelp";

export interface InfoTipProps {
  /** What the control or module does; see `ParameterLabelProps.info`. */
  info: string;

  /** The name of what it explains, for the button's accessible name, e.g. "Mode". */
  label: string;
}

/**
 * An info button that explains a control without a label of its own, e.g. a dropdown or a switch; nothing if help isn't shown. Put it
 * next to the control in a `withInfo` row (`useDefaultStyles`).
 */
export function InfoTip({ info, label }: InfoTipProps) {
  const showHelp = useShowHelp();
  const { t } = useTranslation();

  // on a portal, as in ParameterLabel
  return showHelp ? (
    <InfoButton size="small" inline={false} info={info} aria-label={t("about", { name: label })} />
  ) : null;
}
