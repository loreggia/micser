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
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"], viewport: { width: 1600, height: 900 } } }],
});
