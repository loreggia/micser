import { createContext, useContext } from "react";

/**
 * Whether `ParameterLabel` and `InfoTip` show their info buttons. The UI provides the `showHelp` preference; default: shown.
 */
export const ShowHelpContext = createContext(true);

/**
 * Whether explanations of modules and parameters are shown (the `showHelp` preference).
 */
export function useShowHelp(): boolean {
  return useContext(ShowHelpContext);
}
