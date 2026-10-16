import { InfoLabel, Label } from "@fluentui/react-components";
import type { ReactNode } from "react";
import { useTranslation } from "../i18n/translations";
import { useShowHelp } from "./showHelp";

export interface ParameterLabelProps {
  children: ReactNode;
  htmlFor?: string;

  /**
   * What the parameter does, in a sentence or two for users new to audio processing. Shown in a popover of an info button after the label.
   */
  info?: string;
  disabled?: boolean;
}

/**
 * A small label for a module parameter, with an info button if it has an explanation and help is shown.
 */
export function ParameterLabel({ children, htmlFor, info, disabled }: ParameterLabelProps) {
  const showHelp = useShowHelp();
  const { t } = useTranslation();

  if (!info || !showHelp) {
    return (
      <Label htmlFor={htmlFor} size="small" disabled={disabled}>
        {children}
      </Label>
    );
  }

  return (
    <InfoLabel
      htmlFor={htmlFor}
      size="small"
      disabled={disabled}
      info={info}
      // on a portal: inside a graph node the popover would scale with the zoom and be cut off where the widget scrolls
      infoButton={{ inline: false, "aria-label": t("information") }}
    >
      {children}
    </InfoLabel>
  );
}
