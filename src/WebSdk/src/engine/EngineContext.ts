import { createContext } from "react";
import type { EngineConnection } from "./EngineConnection";

export const EngineConnectionContext = createContext<EngineConnection | undefined>(undefined);
