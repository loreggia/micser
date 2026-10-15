import { makeStyles, tokens } from "@fluentui/react-components";

export const useDefaultStyles = makeStyles({
  dropdown: {
    minWidth: 0,
  },
  column: {
    display: "flex",
    flexDirection: "column",
    gap: tokens.spacingVerticalS,
  },
  /** A control followed by its `InfoTip`. */
  withInfo: {
    display: "flex",
    alignItems: "center",
    gap: tokens.spacingHorizontalXS,
  },
  /** A control that takes the rest of a `withInfo` row, e.g. a dropdown. */
  fill: {
    flexGrow: 1,
    minWidth: 0,
  },
});
