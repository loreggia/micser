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
  /** `ParameterKnob`s side by side, wrapping into rows. */
  knobs: {
    display: "grid",
    gridTemplateColumns: "repeat(auto-fill, minmax(76px, 1fr))",
    columnGap: tokens.spacingHorizontalXS,
    rowGap: tokens.spacingVerticalM,
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
