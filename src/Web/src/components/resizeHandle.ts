import { tokens } from "@fluentui/react-components";

/** The resize handle of modules and subgraphs: a corner inside the node's bottom right. React Flow's handle styles outrank a class. */
export const resizeHandleStyle = {
  width: "12px",
  height: "12px",
  translate: "-100% -100%",
  backgroundColor: "transparent",
  border: "none",
  borderRadius: 0,
  borderRight: `${tokens.strokeWidthThick} solid ${tokens.colorNeutralStroke1}`,
  borderBottom: `${tokens.strokeWidthThick} solid ${tokens.colorNeutralStroke1}`,
};
