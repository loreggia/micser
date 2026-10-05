import { defineConfig, devices } from "@playwright/test";
import { mkdtempSync } from "node:fs";
import { tmpdir } from "node:os";
import { join, resolve } from "node:path";
import { engineUrl, webPort, webUrl } from "./environment";

// the runner creates the folder; its workers get the same one through the environment
process.env.MICSER_E2E_DIRECTORY ??= mkdtempSync(join(tmpdir(), "micser-e2e-"));
const directory = process.env.MICSER_E2E_DIRECTORY;
const root = resolve(import.meta.dirname, "../..");
const isCI = !!process.env.CI;

export default defineConfig({
  testDir: "specs",
  // the tests share one engine, which each test resets
  workers: 1,
  fullyParallel: false,
  forbidOnly: isCI,
  retries: isCI ? 1 : 0,
  reporter: isCI ? [["list"], ["html", { open: "never" }]] : "list",
  use: {
    baseURL: webUrl,
    trace: "retain-on-failure",
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"], viewport: { width: 1600, height: 900 } } }],
  webServer: [
    {
      name: "engine",
      // CI builds the solution first
      command: `dotnet run --project src/Engine --no-launch-profile${isCI ? " --configuration Release --no-build" : ""}`,
      cwd: root,
      url: `${engineUrl}/api/health`,
      env: {
        ASPNETCORE_ENVIRONMENT: "Development",
        Urls: engineUrl,
        Engine__ConfigPath: join(directory, "config.json"),
        Engine__DiscoveryPath: join(directory, "engine.json"),
        Engine__PluginsPath: join(directory, "plugins"),
        Engine__RequireToken: "false",
        Engine__SingleInstance: "false",
      },
      timeout: 180_000,
      gracefulShutdown: { signal: "SIGTERM", timeout: 5_000 },
    },
    {
      name: "web",
      command: `npm run dev -w @micser/web -- --host 127.0.0.1 --port ${webPort} --strictPort`,
      cwd: root,
      url: webUrl,
      env: { MICSER_ENGINE_URL: engineUrl },
      // Vite echoes the browser's console; a server that doesn't start still fails the run
      stdout: "ignore",
      stderr: "ignore",
      timeout: 60_000,
    },
  ],
});
