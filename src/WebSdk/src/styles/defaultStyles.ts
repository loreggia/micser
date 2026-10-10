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
});
