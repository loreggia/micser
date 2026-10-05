import react from "@vitejs/plugin-react";
import { playwright } from "@vitest/browser-playwright";
import { defineConfig } from "vitest/config";

// *.test.ts runs in Node; *.test.tsx and *.browser.test.ts (DOM, storage, module imports) run in Chromium
export default defineConfig({
  test: {
    projects: [
      {
        test: {
          name: "unit",
          environment: "node",
          include: ["src/**/*.test.ts"],
          exclude: ["**/node_modules/**", "**/*.browser.test.ts"],
        },
      },
      {
        plugins: [react()],
        // optimized in one pass: a later pass reloads the tests and can leave two copies of React
        optimizeDeps: {
          include: [
            "react",
            "react/jsx-dev-runtime",
            "react-dom",
            "react-dom/client",
            "@fluentui/react-components",
            "@fluentui/react-icons",
            "@microsoft/signalr",
            "@tanstack/react-query",
            "@xyflow/react",
            "i18next",
            "react-i18next",
            "vitest-browser-react",
          ],
        },
        test: {
          name: "browser",
          include: ["src/**/*.test.tsx", "src/**/*.browser.test.ts"],
          exclude: ["**/node_modules/**"],
          browser: {
            enabled: true,
            headless: true,
            provider: playwright(),
            instances: [{ browser: "chromium" }],
          },
        },
      },
    ],
  },
});
