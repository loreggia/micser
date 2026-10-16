import { makeStyles, tokens } from "@fluentui/react-components";

const knobColumn = "76px";

/** The width of `count` knob columns with the gaps between them. */
const knobColumns = (count: number) => `calc(${count} * ${knobColumn} + ${count - 1} * ${tokens.spacingHorizontalXS})`;

export const useDefaultStyles = makeStyles({
  dropdown: {
    minWidth: 0,
  },
  column: {
    display: "flex",
    flexDirection: "column",
    gap: tokens.spacingVerticalS,
  },
  /**
   * `ParameterKnob`s side by side, wrapping into rows. At least three fit in a row (fewer if there are fewer), so a module at the
   * width its content needs doesn't break a row like Frequency, Gain and Q.
   */
  knobs: {
    display: "grid",
    gridTemplateColumns: `repeat(auto-fill, minmax(${knobColumn}, 1fr))`,
    columnGap: tokens.spacingHorizontalXS,
    rowGap: tokens.spacingVerticalM,
    minWidth: knobColumn,
    ":has(> :nth-child(2))": {
      minWidth: knobColumns(2),
    },
    ":has(> :nth-child(3))": {
      minWidth: knobColumns(3),
    },
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
