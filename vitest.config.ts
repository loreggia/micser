import react from "@vitejs/plugin-react";
import { playwright } from "@vitest/browser-playwright";
import { globSync, readFileSync } from "node:fs";
import { defineConfig } from "vitest/config";

// the per-icon modules of @fluentui/react-icons that the sources import; the package's index has all icons and is too large to load
const iconModules = [
  ...new Set(
    globSync("src/**/*.{ts,tsx}", { exclude: (path) => path.includes("node_modules") }).flatMap((file) =>
      [...readFileSync(file, "utf8").matchAll(/from "(@fluentui\/react-icons\/svg\/[\w-]+)"/g)].map((match) => match[1])
    )
  ),
];

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
            ...iconModules,
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
