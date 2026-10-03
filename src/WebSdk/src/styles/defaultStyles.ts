import { makeStyles, tokens } from "@fluentui/react-components";

export const useDefaultStyles = makeStyles({
  dropdown: {
    minWidth: 0,
  },
  column: {
    display: "flex",
    flexDirection: "column",
    gap: tokens.spacingVerticalS,
    width: "220px",
  },
  band: {
    display: "flex",
    flexDirection: "column",
    gap: tokens.spacingVerticalXS,
    paddingBottom: tokens.spacingVerticalS,
    borderBottom: `${tokens.strokeWidthThin} solid ${tokens.colorNeutralStroke2}`,
  },
  bandHeader: {
    display: "flex",
    justifyContent: "space-between",
    alignItems: "center",
    color: tokens.colorNeutralForeground2,
  },
});
