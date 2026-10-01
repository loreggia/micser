import { useQueryClient } from "@tanstack/react-query";
import { useEffect, useState, type ReactNode } from "react";
import { EngineConnection } from "./EngineConnection";
import { EngineConnectionContext } from "./EngineContext";

/**
 * Connects to the engine while mounted. Needs a QueryClientProvider above it.
 */
export function EngineProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const [connection] = useState(() => new EngineConnection(queryClient));

  useEffect(() => {
    connection.start();
    return () => void connection.stop();
  }, [connection]);

  return <EngineConnectionContext.Provider value={connection}>{children}</EngineConnectionContext.Provider>;
}
