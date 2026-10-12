import { defineConfig, devices } from "@playwright/test";

const isCI = !!process.env.CI;

// Each worker starts its own engine and Vite server on free ports (see the servers fixture), so the tests run in parallel.
export default defineConfig({
  testDir: "specs",
  globalSetup: "./globalSetup.ts",
  fullyParallel: true,
  forbidOnly: isCI,
  retries: isCI ? 1 : 0,
  reporter: isCI ? [["list"], ["html", { open: "never" }]] : "list",
  use: {
    trace: "retain-on-failure",
    // the UI follows the browser's language; tests that need another one set it
    locale: "en-US",
  },
  projects: [
    { name: "chromium", use: { ...devices["Desktop Chrome"], viewport: { width: 1600, height: 900 } } },
    // the Linux shell renders with WebKitGTK, so on Linux the smoke specs also run in Playwright's WebKit
    ...(process.platform === "linux"
      ? [
          {
            name: "webkit",
            testMatch: [
              "app.spec.ts",
              "connections.spec.ts",
              "modules.spec.ts",
              "settings.spec.ts",
              "shell.spec.ts",
              "subgraphs.spec.ts",
            ],
            // Playwright's WebKit claims macOS, which makes React Flow expect Meta instead of Control; WebKitGTK says Linux
            use: {
              browserName: "webkit" as const,
              viewport: { width: 1600, height: 900 },
              userAgent:
                "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/26.6 Safari/605.1.15",
            },
          },
        ]
      : []),
  ],
});
