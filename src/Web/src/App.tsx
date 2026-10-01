import { useEffect, useState } from "react";
import { plugins } from "./plugins";

type EngineStatus = "connecting" | "ok" | "unreachable";

export function App() {
  const [status, setStatus] = useState<EngineStatus>("connecting");

  useEffect(() => {
    const controller = new AbortController();

    fetch("/api/health", { signal: controller.signal })
      .then((response) => setStatus(response.ok ? "ok" : "unreachable"))
      .catch(() => {
        if (!controller.signal.aborted) {
          setStatus("unreachable");
        }
      });

    return () => controller.abort();
  }, []);

  const widgetCount = plugins.reduce((count, plugin) => count + plugin.widgets.length, 0);

  return (
    <main>
      <h1>Micser</h1>
      <p>Engine: {status}</p>
      <p>
        Plugins: {plugins.map((plugin) => plugin.name).join(", ")} ({widgetCount} widgets)
      </p>
    </main>
  );
}
