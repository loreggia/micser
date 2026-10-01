import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";

// In development the engine runs separately (see src/Engine/appsettings.json for its address).
const engineUrl = process.env.MICSER_ENGINE_URL ?? "http://127.0.0.1:5080";

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      "/api": engineUrl,
      "/hubs": { target: engineUrl, ws: true },
    },
  },
});
