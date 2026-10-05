import { mkdtempSync, rmSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { buildEngine, buildTestPluginPackage } from "./servers";

/** Builds the engine, which each worker starts from the build output, and the test plugin's package. */
export default function globalSetup() {
  const configuration = process.env.CI ? "Release" : "Debug";
  const directory = mkdtempSync(join(tmpdir(), "micser-e2e-package-"));

  process.env.MICSER_E2E_ENGINE = buildEngine(configuration);
  process.env.MICSER_E2E_PLUGIN_PACKAGE = buildTestPluginPackage(configuration, directory);

  return () => rmSync(directory, { recursive: true, force: true });
}
