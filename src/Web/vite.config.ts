import react from "@vitejs/plugin-react";
import { resolve } from "node:path";
import { defineConfig } from "vite";
import { sharedModulesPlugin } from "./vite/sharedModules.ts";
import { workspacePluginsPlugin } from "./vite/workspacePlugins.ts";

// In development the engine runs separately (see src/Engine/appsettings.json for its address).
const engineUrl = process.env.MICSER_ENGINE_URL ?? "http://127.0.0.1:5080";

export default defineConfig({
  plugins: [react(), sharedModulesPlugin(), workspacePluginsPlugin(resolve(import.meta.dirname, "../Plugins"))],
  build: {
    rolldownOptions: {
      output: {
        // the large libraries in their own chunks, which only change when the dependencies do
        codeSplitting: {
          groups: [
            { name: "react", test: /node_modules[\\/](react|react-dom|scheduler)[\\/]/, priority: 30 },
            { name: "fluent-icons", test: /node_modules[\\/]@fluentui[\\/]react-icons[\\/]/, priority: 30 },
            {
              name: "fluent",
              test: /node_modules[\\/](@fluentui|@griffel|tabster|keyborg|@floating-ui)[\\/]/,
              priority: 20,
            },
            { name: "xyflow", test: /node_modules[\\/](@xyflow|d3-[^\\/]+|classcat|zustand)[\\/]/, priority: 20 },
            { name: "vendor", test: /node_modules[\\/]/, priority: 10 },
          ],
        },
      },
    },
  },
  server: {
    proxy: {
      "/api": engineUrl,
      "/hubs": { target: engineUrl, ws: true },
      // widget bundles of plugins outside this repository
      "/plugins": engineUrl,
    },
  },
});
